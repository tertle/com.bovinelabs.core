using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace BovineLabs.Core.Graphics
{
    internal enum OperationType : int
    {
        Upload = 0,
        Matrix_4x4 = 1,
        Matrix_Inverse_4x4 = 2,
        Matrix_3x4 = 3,
        Matrix_Inverse_3x4 = 4,
        StridedUpload = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Operation
    {
        public uint type;
        public uint srcOffset;
        public uint srcStride;
        public uint dstOffset;
        public uint dstOffsetExtra;
        public int dstStride;
        public uint size;
        public uint count;
    }

    internal unsafe struct MappedBuffer
    {
        public byte* m_Data;
        public long m_Marker;
        public int m_BufferID;

        public static long PackMarker(long operationOffset, long dataOffset)
        {
            return (dataOffset << 32) | (operationOffset & 0xFFFFFFFF);
        }

        public static void UnpackMarker(long marker, out long operationOffset, out long dataOffset)
        {
            operationOffset = marker & 0xFFFFFFFF;
            dataOffset = (marker >> 32) & 0xFFFFFFFF;
        }

        public int TryAllocUploads(int size, int requestedCount, out byte* ptr, out int operationOffset, out int dataOffset)
        {
            var operationSize = UnsafeUtility.SizeOf<Operation>();
            long originalMarker;
            long newMarker;
            long currentOperationOffset;
            long currentDataOffset;
            int count;
            do
            {
                originalMarker = Interlocked.Read(ref m_Marker);
                UnpackMarker(originalMarker, out currentOperationOffset, out currentDataOffset);
                var capacity = (currentDataOffset - currentOperationOffset) / (operationSize + (long)size);
                count = (int)(capacity < requestedCount ? capacity : requestedCount);
                if (count == 0)
                {
                    ptr = null;
                    operationOffset = 0;
                    dataOffset = 0;
                    return 0;
                }

                newMarker = PackMarker(currentOperationOffset + ((long)count * operationSize), currentDataOffset - ((long)count * size));
            }
            while (Interlocked.CompareExchange(ref m_Marker, newMarker, originalMarker) != originalMarker);

            ptr = m_Data;
            operationOffset = (int)currentOperationOffset;
            dataOffset = (int)(currentDataOffset - ((long)count * size));
            return count;
        }

        public bool TryAlloc(int operationSize, int dataSize, out byte* ptr, out int operationOffset, out int dataOffset)
        {
            long originalMarker;
            long newMarker;
            long currOperationOffset;
            long currDataOffset;
            do
            {
                // Read the marker as is right now
                originalMarker = Interlocked.Read(ref m_Marker);
                UnpackMarker(originalMarker, out currOperationOffset, out currDataOffset);

                // Calculate the new offsets for operation and data
                // Operations are stored in the beginning of the buffer
                // Data is stored at the end of the buffer
                var newOperationOffset = currOperationOffset + operationSize;
                var newDataOffset = currDataOffset - dataSize;

                // Check if there was enough space in the buffer for this allocation
                if (newDataOffset < newOperationOffset)
                {
                    // Not enough space, return false
                    ptr = null;
                    operationOffset = 0;
                    dataOffset = 0;
                    return false;
                }

                newMarker = PackMarker(newOperationOffset, newDataOffset);

                // Finally we try to CAS the new marker in.
                // If anyone has allocated from the buffer in the meantime this will fail and the loop will rerun
            } while (Interlocked.CompareExchange(ref m_Marker, newMarker, originalMarker) != originalMarker);

            // Now we have succeeded in getting a data slot out and can return true.
            ptr = m_Data;
            operationOffset = (int)currOperationOffset;
            dataOffset = (int)(currDataOffset - dataSize);
            return true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ThreadedSparseUploaderData
    {
        [NativeDisableUnsafePtrRestriction] public MappedBuffer* m_Buffers;
        public int m_NumBuffers;
        public int m_CurrBuffer;
    }

    /// <summary>
    /// An unmanaged and Burst-compatible interface for BovineSparseUploader.
    /// </summary>
    /// <remarks>
    /// This should be created each frame by a call to BovineSparseUploader.Begin and is later returned by a call to BovineSparseUploader.EndAndCommit.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ThreadedBovineSparseUploader
    {
        // TODO: safety handle?
        [NativeDisableUnsafePtrRestriction] internal ThreadedSparseUploaderData* m_Data;

        /// <summary>
        /// Indicates whether the BovineSparseUploader is valid and can be used.
        /// </summary>
        public bool IsValid => m_Data != null;

        private bool TryAlloc(int operationSize, int dataSize, out byte* ptr, out int operationOffset, out int dataOffset)
        {
            // Fetch current buffer and ensure we are not already out of GPU buffers to allocate from;
            var numBuffers = m_Data->m_NumBuffers;
            var buffer = m_Data->m_CurrBuffer;
            if (buffer < numBuffers)
            {
                do
                {
                    // Try to allocate from the current buffer
                    if (m_Data->m_Buffers[buffer].TryAlloc(operationSize, dataSize, out var p, out var op, out var d))
                    {
                        // Success, we can return true at onnce
                        ptr = p;
                        operationOffset = op;
                        dataOffset = d;
                        return true;
                    }

                    // Try to increment the buffer.
                    // If someone else has done this while we where trying to alloc we will use their
                    // value and du another iteration. Otherwise we will use our new value
                    buffer = Interlocked.CompareExchange(ref m_Data->m_CurrBuffer, buffer + 1, buffer);
                } while (buffer < m_Data->m_NumBuffers);
            }

            // We have run out of buffers, return false
            ptr = null;
            operationOffset = 0;
            dataOffset = 0;
            return false;
        }

        /// <summary>
        /// Adds a new pending upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source pointer.
        /// </remarks>
        /// <param name="src">The source pointer of data to upload.</param>
        /// <param name="size">The amount of data, in bytes, to read from the source pointer.</param>
        /// <param name="offsetInBytes">The destination offset of the data in the GPU buffer.</param>
        /// <param name="repeatCount">The number of times to repeat the source data in the destination buffer when uploading.</param>
        public void AddUpload(void* src, int size, int offsetInBytes, int repeatCount = 1)
        {
            var opsize = UnsafeUtility.SizeOf<Operation>();
            var allocSucceeded = TryAlloc(opsize, size, out var dst, out var operationOffset, out var dataOffset);

            if (!allocSucceeded)
            {
                Debug.Log("BovineSparseUploader failed to allocate upload memory for AddUpload operation");
                return;
            }

            if (repeatCount <= 0)
                repeatCount = 1;

            // TODO: Vectorized memcpy
            UnsafeUtility.MemCpy(dst + dataOffset, src, size);
            var op = new Operation
            {
                type = (uint)OperationType.Upload,
                srcOffset = (uint)dataOffset,
                dstOffset = (uint)offsetInBytes,
                dstOffsetExtra = 0,
                size = (uint)size,
                count = (uint)repeatCount
            };
            UnsafeUtility.MemCpy(dst + operationOffset, &op, opsize);
        }

        /// <summary>Reserves uninitialized staging memory for a producer to fill directly.</summary>
        /// <remarks>
        /// Size must be positive and divisible by four. Destination offsets must be nonnegative multiples of four.
        /// The returned memory is write-only and only four-byte alignment is guaranteed. Fully initialize it before EndAndCommit.
        /// A successful reservation cannot be cancelled. Complete every producer before commit; never retain the pointer afterward.
        /// Budget the payload and descriptor in Begin just as for AddUpload. Failure returns false and a null pointer without reserving an operation.
        /// </remarks>
        public bool TryReserveUpload(int size, int offsetInBytes, out void* payload, int repeatCount = 1)
        {
            var operationSize = UnsafeUtility.SizeOf<Operation>();
            if (!TryAlloc(operationSize, size, out var dst, out var operationOffset, out var dataOffset))
            {
                payload = null;
                return false;
            }

            var operation = new Operation
            {
                type = (uint)OperationType.Upload,
                srcOffset = (uint)dataOffset,
                dstOffset = (uint)offsetInBytes,
                dstOffsetExtra = 0,
                size = (uint)size,
                count = (uint)math.max(1, repeatCount)
            };
            UnsafeUtility.MemCpy(dst + operationOffset, &operation, operationSize);
            payload = dst + dataOffset;
            return true;
        }

        /// <summary>Stages independent equal-sized uploads using bounded shared reservations.</summary>
        /// <remarks>
        /// Source and destination ranges must be valid. Size must be positive and divisible by four.
        /// Final destination offsets must be nonnegative multiples of four representable by int.
        /// Count must be nonnegative. Begin bounds count every payload and descriptor separately, with size as the largest individual payload.
        /// Complete all producers before EndAndCommit. A zero count reads neither pointer. Repeat counts below one are treated as one.
        /// </remarks>
        /// <param name="src">Source of the first payload, copied during this call.</param>
        /// <param name="size">Positive byte size of each payload.</param>
        /// <param name="sourceStride">Nonnegative source step in bytes; zero reuses the same payload.</param>
        /// <param name="offsets">Pointer to destination byte offsets.</param>
        /// <param name="offsetStride">Positive step between offsets, measured in integers.</param>
        /// <param name="count">Number of independent upload operations.</param>
        /// <param name="repeatCount">Number of copies of each payload at its destination.</param>
        /// <param name="destinationBias">Byte offset added to every destination offset.</param>
        public void AddUploads(
            void* src, int size, int sourceStride, int* offsets, int offsetStride, int count, int repeatCount = 1, int destinationBias = 0)
        {
            var source = (byte*)src;
            var operationSize = UnsafeUtility.SizeOf<Operation>();
            repeatCount = math.max(1, repeatCount);
            while (count > 0)
            {
                var requestedCount = math.min(count, 64);
                var buffer = m_Data->m_CurrBuffer;
                var allocatedCount = 0;
                byte* destination = null;
                var operationOffset = 0;
                var dataOffset = 0;
                while (buffer < m_Data->m_NumBuffers)
                {
                    allocatedCount = m_Data->m_Buffers[buffer].TryAllocUploads(size, requestedCount, out destination, out operationOffset, out dataOffset);
                    if (allocatedCount > 0)
                    {
                        break;
                    }

                    // Advance only when a single operation cannot fit; Begin budgets single-operation tail waste.
                    buffer = Interlocked.CompareExchange(ref m_Data->m_CurrBuffer, buffer + 1, buffer);
                }

                if (allocatedCount == 0)
                {
                    Debug.Log("BovineSparseUploader failed to allocate upload memory for AddUploads operation");
                    return;
                }

                for (var i = 0; i < allocatedCount; i++)
                {
                    var payloadOffset = dataOffset + (i * size);
                    UnsafeUtility.MemCpy(destination + payloadOffset, source + ((long)i * sourceStride), size);
                    var operation = new Operation
                    {
                        type = (uint)OperationType.Upload,
                        srcOffset = (uint)payloadOffset,
                        dstOffset = (uint)(offsets[(long)i * offsetStride] + destinationBias),
                        size = (uint)size,
                        count = (uint)repeatCount,
                    };
                    UnsafeUtility.MemCpy(destination + operationOffset + (i * operationSize), &operation, operationSize);
                }

                source += (long)allocatedCount * sourceStride;
                offsets += (long)allocatedCount * offsetStride;
                count -= allocatedCount;
            }
        }

        /// <summary>
        /// Adds a new pending upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source value.
        /// </remarks>
        /// <param name="val">The source data to upload.</param>
        /// <param name="offsetInBytes">The destination offset of the data in the GPU buffer.</param>
        /// <param name="repeatCount">The number of times to repeat the source data in the destination buffer when uploading.</param>
        /// <typeparam name="T">Any unmanaged simple type.</typeparam>
        public void AddUpload<T>(T val, int offsetInBytes, int repeatCount = 1) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            AddUpload(&val, size, offsetInBytes, repeatCount);
        }

        /// <summary>
        /// Adds a new pending upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source array.
        /// </remarks>
        /// <param name="array">The source array of data to upload.</param>
        /// <param name="offsetInBytes">The destination offset of the data in the GPU buffer.</param>
        /// <param name="repeatCount">The number of times to repeat the source data in the destination buffer when uploading.</param>
        /// <typeparam name="T">Any unmanaged simple type.</typeparam>
        public void AddUpload<T>(NativeArray<T> array, int offsetInBytes, int repeatCount = 1) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>() * array.Length;
            AddUpload(array.GetUnsafeReadOnlyPtr(), size, offsetInBytes, repeatCount);
        }

        /// <summary>
        /// Options for the type of matrix to use in matrix uploads.
        /// </summary>
        public enum MatrixType
        {
            /// <summary>
            /// A float4x4 matrix.
            /// </summary>
            MatrixType4x4,

            /// <summary>
            /// A float3x4 matrix.
            /// </summary>
            MatrixType3x4,
        }

        private void MatrixUploadHelper(void* src, int numMatrices, int offset, int offsetInverse, MatrixType srcType, MatrixType dstType)
        {
            var size = numMatrices * sizeof(float3x4);
            var opsize = UnsafeUtility.SizeOf<Operation>();

            var allocSucceeded = TryAlloc(opsize, size, out var dst, out var operationOffset, out var dataOffset);

            if (!allocSucceeded)
            {
                Debug.Log("BovineSparseUploader failed to allocate upload memory for AddMatrixUpload operation");
                return;
            }

            if (srcType == MatrixType.MatrixType4x4)
            {
                var srcLocal = (byte*)src;
                var dstLocal = dst + dataOffset;
                for (int i = 0; i < numMatrices; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        UnsafeUtility.MemCpy(dstLocal, srcLocal, 12);
                        dstLocal += 12;
                        srcLocal += 16;
                    }
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dst + dataOffset, src, size);
            }

            var uploadType = (offsetInverse == -1) ? (uint)OperationType.Matrix_4x4 : (uint)OperationType.Matrix_Inverse_4x4;
            uploadType += (dstType == MatrixType.MatrixType3x4) ? 2u : 0u;

            var op = new Operation
            {
                type = uploadType,
                srcOffset = (uint)dataOffset,
                dstOffset = (uint)offset,
                dstOffsetExtra = (uint)offsetInverse,
                size = (uint)size,
                count = 1,
            };
            UnsafeUtility.MemCpy(dst + operationOffset, &op, opsize);
        }

        /// <summary>
        /// Adds a new pending matrix upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source pointer.
        /// </remarks>
        /// <param name="src">A pointer to a memory area that contains matrices of the type specified by srcType.</param>
        /// <param name="numMatrices">The number of matrices to upload.</param>
        /// <param name="offset">The destination offset of the copy part of the upload operation.</param>
        /// <param name="srcType">The source matrix format.</param>
        /// <param name="dstType">The destination matrix format.</param>
        public void AddMatrixUpload(void* src, int numMatrices, int offset, MatrixType srcType, MatrixType dstType)
        {
            MatrixUploadHelper(src, numMatrices, offset, -1, srcType, dstType);
        }

        /// <summary>
        /// Adds a new pending matrix upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source pointer.
        ///
        /// The upload operation automatically inverts matrices during the upload operation and it then stores the inverted matrices in a
        /// separate offset in the GPU buffer.
        /// </remarks>
        /// <param name="src">A pointer to a memory area that contains matrices of the type specified by srcType.</param>
        /// <param name="numMatrices">The number of matrices to upload.</param>
        /// <param name="offset">The destination offset of the copy part of the upload operation.</param>
        /// <param name="offsetInverse">The destination offset of the inverse part of the upload operation.</param>
        /// <param name="srcType">The source matrix format.</param>
        /// <param name="dstType">The destination matrix format.</param>
        public void AddMatrixUploadAndInverse(void* src, int numMatrices, int offset, int offsetInverse, MatrixType srcType, MatrixType dstType)
        {
            MatrixUploadHelper(src, numMatrices, offset, offsetInverse, srcType, dstType);
        }

        /// <summary>
        /// Adds a new pending upload operation to execute when you call BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// When this operation executes, the BovineSparseUploader copies data from the source pointer.
        ///
        /// The upload operations reads data with the specified source stride from the source pointer and then stores the data with the specified destination stride.
        /// </remarks>
        /// <param name="src">The source data pointer.</param>
        /// <param name="elemSize">The size of each data element to upload.</param>
        /// <param name="srcStride">The stride of each data element as stored in the source pointer.</param>
        /// <param name="count">The number of data elements to upload.</param>
        /// <param name="dstOffset">The destination offset</param>
        /// <param name="dstStride">The destination stride</param>
        public void AddStridedUpload(void* src, uint elemSize, uint srcStride, uint count, uint dstOffset, int dstStride)
        {
            if (count == 0)
                return;

            int opSize = UnsafeUtility.SizeOf<Operation>();
            uint dataSize = count * elemSize;

            var allocSucceeded = TryAlloc(opSize, (int)dataSize, out var dst, out var operationOffset, out var dataOffset);

            if (!allocSucceeded)
            {
                Debug.Log("BovineSparseUploader failed to allocate upload memory for AddStridedUpload operation");
                return;
            }

            if (srcStride == elemSize)
                UnsafeUtility.MemCpy(dst + dataOffset, src, dataSize);
            else
                UnsafeUtility.MemCpyStride(dst + dataOffset, (int)elemSize, src, (int)srcStride, (int)elemSize, (int)count);
            var op = new Operation
            {
                type = (uint)OperationType.StridedUpload,
                srcOffset = (uint)dataOffset,
                srcStride = elemSize,
                dstOffset = (uint)dstOffset,
                dstOffsetExtra = 0,
                dstStride = dstStride,
                size = elemSize,
                count = count,
            };
            UnsafeUtility.MemCpy(dst + operationOffset, &op, opSize);
        }
    }

    internal class BufferPool : IDisposable
    {
        private List<GraphicsBuffer> m_Buffers;
        private Stack<int> m_FreeBufferIds;
        private Stack<int> m_BuffersReleased;

        private int m_Count;
        private int m_Stride;
        private GraphicsBuffer.Target m_Target;
        private GraphicsBuffer.UsageFlags m_UsageFlags;


        public BufferPool(int count, int stride, GraphicsBuffer.Target target, GraphicsBuffer.UsageFlags usageFlags)
        {
            m_Buffers = new List<GraphicsBuffer>();
            m_FreeBufferIds = new Stack<int>();
            m_BuffersReleased = new Stack<int>();

            m_Count = count;
            m_Stride = stride;
            m_Target = target;
            m_UsageFlags = usageFlags;
        }

        public void Dispose()
        {
            for (int i = 0; i < m_Buffers.Count; ++i)
            {
                if (m_Buffers[i].IsValid())
                {
                    m_Buffers[i].Dispose();
                }
            }
        }

        private int AllocateBuffer()
        {
            var cb = new GraphicsBuffer(m_Target, m_UsageFlags, m_Count, m_Stride);
            cb.name = "SparseUploaderBuffer";
            if (m_BuffersReleased.Count > 0)
            {
                var id = m_BuffersReleased.Pop();
                m_Buffers[id] = cb;
                return id;
            }
            else
            {
                var id = m_Buffers.Count;
                m_Buffers.Add(cb);
                return id;
            }
        }

        public int GetBufferId()
        {
            if (m_FreeBufferIds.Count == 0)
                return AllocateBuffer();

            return m_FreeBufferIds.Pop();
        }

        public GraphicsBuffer GetBufferFromId(int id)
        {
            return m_Buffers[id];
        }

        public void PutBufferId(int id)
        {
            m_FreeBufferIds.Push(id);
        }

        /*
         * Prune free buffers to allow up to maxMemoryToRetainInBytes to remain.
         * Note that this will only release buffers that are marked free, so the actual memory retained might be higher than requested
         */
        public void PruneFreeBuffers(int maxMemoryToRetainInBytes)
        {
            int memoryToFree = TotalBufferSize - maxMemoryToRetainInBytes;
            if (memoryToFree <= 0) return;

            while (memoryToFree > 0 && m_FreeBufferIds.Count > 0)
            {
                var id = m_FreeBufferIds.Pop();
                var buffer = GetBufferFromId(id);
                buffer.Dispose();
                m_BuffersReleased.Push(id);
                memoryToFree -= m_Count * m_Stride;
            }
        }

        public int TotalBufferCount => m_Buffers.Count - m_BuffersReleased.Count;
        public int TotalBufferSize => TotalBufferCount * m_Count * m_Stride;
    }

    /// <summary>
    /// Represents BovineSparseUploader statistics.
    /// </summary>
    public struct BovineSparseUploaderStats
    {
        /// <summary>
        /// The amount of GPU memory the BovineSparseUploader uses internally.
        /// </summary>
        /// <remarks>
        /// This value doesn't include memory in the managed GPU buffer that you pass into the BovineSparseUploader on construction,
        /// or when you use BovineSparseUploader.ReplaceBuffer.
        /// </remarks>
        public long BytesGPUMemoryUsed;

        /// <summary>
        /// The amount of memory the BovineSparseUploader used to upload during the current frame.
        /// </summary>
        public long BytesGPUMemoryUploadedCurr;

        /// <summary>
        /// The highest amount of memory the BovineSparseUploader used for upload during a previous frame.
        /// </summary>
        public long BytesGPUMemoryUploadedMax;
    }

    /// <summary>
    /// Provides utility methods that you can use to upload data into GPU memory.
    /// </summary>
    /// <remarks>
    /// To add uploads from jobs, use a ThreadedBovineSparseUploader which you can create using BovineSparseUploader.Begin.
    /// If you add uploads from jobs, the ThreadedBovineSparseUploader submits them to the GPU in a series of compute shader dispatches when you call BovineSparseUploader.EndAndCommit.
    /// </remarks>
    public unsafe struct BovineSparseUploader : IDisposable
    {
        const int k_MaxThreadGroupsPerDispatch = 65535;

        int m_BufferChunkSize;

        GraphicsBuffer m_DestinationBuffer;

        BufferPool m_UploadBufferPool;

        NativeArray<MappedBuffer> m_MappedBuffers;

        private long m_CurrentFrameUploadSize;
        private long m_MaxUploadSize;

        class FrameData
        {
            public Stack<int> m_Buffers;
            public GraphicsFence m_Fence;

            public FrameData()
            {
                m_Buffers = new Stack<int>();
            }
        }

        Stack<FrameData> m_FreeFrameData;
        List<FrameData> m_FrameData;
        readonly bool m_UseGraphicsFence;

        ThreadedSparseUploaderData* m_ThreadData;

        ComputeShader m_SparseUploaderShader;
        int m_CopyKernelIndex;
        int m_ReplaceKernelIndex;

        int m_SrcBufferID;
        int m_DstBufferID;
        int m_OperationsBaseID;
        int m_ReplaceOperationSize;

        int m_RequestedUploadBufferPoolMaxSizeBytes;
        bool m_PruneUploadBufferPool;

        /// <summary>
        /// Constructs a new sparse uploader with the specified buffer as the target.
        /// </summary>
        /// <param name="destinationBuffer">The target buffer to write uploads into.</param>
        /// <param name="bufferChunkSize">The upload buffer chunk size.</param>
        /// <param name="useGraphicsFence">Retire staging only after GPU completion, allowing multiple commits per rendered frame. Requires graphics-fence support.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="destinationBuffer"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="destinationBuffer"/> is invalid or has an incompatible target type.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bufferChunkSize"/> is less than or equal to zero.</exception>
        public BovineSparseUploader(GraphicsBuffer destinationBuffer, int bufferChunkSize = 16 * 1024 * 1024, bool useGraphicsFence = false)
        {
            if (destinationBuffer == null)
                throw new ArgumentNullException(nameof(destinationBuffer), "Destination buffer cannot be null.");

            if (!destinationBuffer.IsValid())
                throw new ArgumentException("Destination buffer is not in a valid state. The buffer may have been disposed or not properly initialized.", nameof(destinationBuffer));

            if ((destinationBuffer.target & GraphicsBuffer.Target.Raw) != GraphicsBuffer.Target.Raw)
                throw new ArgumentException("Destination buffer must have Raw target type for sparse uploading.", nameof(destinationBuffer));

            if (bufferChunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferChunkSize), bufferChunkSize, "Buffer chunk size must be greater than zero.");

            if (useGraphicsFence && !SystemInfo.supportsGraphicsFence)
                throw new NotSupportedException("Graphics-fence retirement requires graphics-fence support.");

            m_BufferChunkSize = bufferChunkSize;
            m_UseGraphicsFence = useGraphicsFence;

            m_DestinationBuffer = destinationBuffer;

            m_UploadBufferPool = new BufferPool(m_BufferChunkSize / 4, 4, GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite);
            m_MappedBuffers = new NativeArray<MappedBuffer>();
            m_FreeFrameData = new Stack<FrameData>();
            m_FrameData = new List<FrameData>();

            m_ThreadData = (ThreadedSparseUploaderData*)UnsafeUtility.Malloc(sizeof(ThreadedSparseUploaderData),
                UnsafeUtility.AlignOf<ThreadedSparseUploaderData>(), Allocator.Persistent);
            m_ThreadData->m_Buffers = null;
            m_ThreadData->m_NumBuffers = 0;
            m_ThreadData->m_CurrBuffer = 0;

            m_SparseUploaderShader = Resources.Load<ComputeShader>("BovineLabs/CoreSparseUploader");
            m_CopyKernelIndex = m_SparseUploaderShader.FindKernel("CopyKernel");
            m_ReplaceKernelIndex = m_SparseUploaderShader.FindKernel("ReplaceKernel");

            m_SrcBufferID = Shader.PropertyToID("srcBuffer");
            m_DstBufferID = Shader.PropertyToID("dstBuffer");
            m_OperationsBaseID = Shader.PropertyToID("operationsBase");
            m_ReplaceOperationSize = Shader.PropertyToID("replaceOperationSize");

            m_CurrentFrameUploadSize = 0;
            m_MaxUploadSize = 0;

            m_RequestedUploadBufferPoolMaxSizeBytes = 0;
            m_PruneUploadBufferPool = false;
        }

        /// <summary>
        /// Disposes of the BovineSparseUploader.
        /// </summary>
        public void Dispose()
        {
            m_UploadBufferPool.Dispose();
            UnsafeUtility.Free(m_ThreadData, Allocator.Persistent);
        }

        /// <summary>
        /// Replaces the destination GPU buffer with a new one.
        /// </summary>
        /// <remarks>
        /// If the existing buffer is non-null and copyFromPrevious is true, this method
        /// dispatches a copy operation that copies data from the previous buffer to the new one.
        ///
        /// This is useful when the persistent storage buffer needs to grow.
        /// </remarks>
        /// <param name="buffer">The new buffer to replace the old one with.</param>
        /// <param name="copyFromPrevious">Indicates whether to copy the contents of the old buffer to the new buffer.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> is invalid or has an incompatible target type.</exception>
        public void ReplaceBuffer(GraphicsBuffer buffer, bool copyFromPrevious = false)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "Destination buffer cannot be null.");

            if (!buffer.IsValid())
                throw new ArgumentException("Destination buffer is not in a valid state. The buffer may have been disposed or not properly initialized.", nameof(buffer));

            if ((buffer.target & GraphicsBuffer.Target.Raw) != GraphicsBuffer.Target.Raw)
                throw new ArgumentException("Destination buffer must have Raw target type for sparse uploading.", nameof(buffer));

            if (copyFromPrevious && m_DestinationBuffer != null)
            {
                // Since we have no code such as Graphics.CopyBuffer(dst, src) currently
                // we have to do this ourselves in a compute shader
                var srcSize = m_DestinationBuffer.count * m_DestinationBuffer.stride;
                m_SparseUploaderShader.SetBuffer(m_ReplaceKernelIndex, m_SrcBufferID, m_DestinationBuffer);
                m_SparseUploaderShader.SetBuffer(m_ReplaceKernelIndex, m_DstBufferID, buffer);
                m_SparseUploaderShader.SetInt(m_ReplaceOperationSize, srcSize);

                m_SparseUploaderShader.Dispatch(m_ReplaceKernelIndex, 1, 1, 1);
            }

            m_DestinationBuffer = buffer;
        }

        internal static int NumFramesInFlight
        {
            get
            {
                // The number of frames in flight at the same time
                // depends on the Graphics device that we are using.
                // This number tells how long we need to keep the buffers
                // for a given frame alive. For example, if this is 4,
                // we can reclaim the buffers for a frame after 4 frames have passed.
                int numFrames = 0;

                switch (SystemInfo.graphicsDeviceType)
                {
                    case GraphicsDeviceType.Vulkan:
                    case GraphicsDeviceType.Direct3D11:
                    case GraphicsDeviceType.Direct3D12:
                    case GraphicsDeviceType.PlayStation4:
                    case GraphicsDeviceType.PlayStation5:
                    case GraphicsDeviceType.XboxOne:
                    case GraphicsDeviceType.GameCoreXboxOne:
                    case GraphicsDeviceType.GameCoreXboxSeries:
                    case GraphicsDeviceType.OpenGLCore:

                    // OpenGL ES 2.0 is no longer supported in Unity 2023.1 and later
#if !UNITY_2023_1_OR_NEWER
                    case GraphicsDeviceType.OpenGLES2:
#endif

                    case GraphicsDeviceType.OpenGLES3:
                    case GraphicsDeviceType.PlayStation5NGGC:
                        numFrames = 3;
                        break;
                    case GraphicsDeviceType.Switch:
                    case GraphicsDeviceType.Metal:
                    default:
                        numFrames = 4;
                        break;
                }

                // Use at least as many frames as the quality settings have, but use a platform
                // specific lower limit in any case.
                numFrames = math.max(numFrames, QualitySettings.maxQueuedFrames);

                return numFrames;
            }
        }

        private void RecoverBuffers()
        {
            int numFree = 0;

            if (m_UseGraphicsFence)
            {
                while (numFree < m_FrameData.Count && m_FrameData[numFree].m_Fence.passed)
                {
                    numFree++;
                }
            }
            else
            {
                // The default path retains the upstream one-commit-per-frame contract.
                // Include one extra frame for overlapping CPU submission.
                int maxBufferedFrames = NumFramesInFlight + 1;
                numFree = math.max(0, m_FrameData.Count - maxBufferedFrames);
            }

            for (int i = 0; i < numFree; ++i)
            {
                while (m_FrameData[i].m_Buffers.Count > 0)
                {
                    var buffer = m_FrameData[i].m_Buffers.Pop();
                    m_UploadBufferPool.PutBufferId(buffer);
                }
                m_FreeFrameData.Push(m_FrameData[i]);
            }

            if (numFree > 0)
            {
                m_FrameData.RemoveRange(0, numFree);
            }
        }

        /// <summary>
        /// Begins a new upload frame and returns a new ThreadedBovineSparseUploader that is valid until the next call to
        /// BovineSparseUploader.EndAndCommit.
        /// </summary>
        /// <remarks>
        /// You must follow this method with a call to BovineSparseUploader.EndAndCommit later in the frame. You must also pass
        /// the returned value from a Begin method to the next BovineSparseUploader.EndAndCommit.
        /// </remarks>
        /// <param name="maxDataSizeInBytes">An upper bound of total data size that you want to upload this frame.</param>
        /// <param name="biggestDataUpload">The size of the largest upload operation that will occur.</param>
        /// <param name="maxOperationCount">An upper bound of the total number of upload operations that will occur this frame.</param>
        /// <returns>Returns a new ThreadedBovineSparseUploader that must be passed to BovineSparseUploader.EndAndCommit later.</returns>
        public ThreadedBovineSparseUploader Begin(int maxDataSizeInBytes, int biggestDataUpload, int maxOperationCount)
        {
            // First: recover all buffers from the previous frames (if any)
            RecoverBuffers();

            // Second: calculate total size needed this frame, allocate buffers and map what is needed
            var operationSize = UnsafeUtility.SizeOf<Operation>();
            var maxOperationSizeInBytes = maxOperationCount * operationSize;
            var sizeNeeded = maxOperationSizeInBytes + maxDataSizeInBytes;
            var bufferSizeWithMaxPaddingRemoved = m_BufferChunkSize - operationSize - biggestDataUpload;
            // A complete batch that fits one page cannot lose space to page transitions.
            var numBuffersNeeded = sizeNeeded <= m_BufferChunkSize
                ? (sizeNeeded == 0 ? 0 : 1)
                : (sizeNeeded + bufferSizeWithMaxPaddingRemoved - 1) / bufferSizeWithMaxPaddingRemoved;

            if (numBuffersNeeded < 0)
                numBuffersNeeded = 0;

            m_CurrentFrameUploadSize = sizeNeeded;
            if (m_CurrentFrameUploadSize > m_MaxUploadSize)
                m_MaxUploadSize = m_CurrentFrameUploadSize;

            m_MappedBuffers = new NativeArray<MappedBuffer>(numBuffersNeeded, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for(int i = 0; i < numBuffersNeeded; ++i)
            {
                var id = m_UploadBufferPool.GetBufferId();
                var cb = m_UploadBufferPool.GetBufferFromId(id);
                var data = cb.LockBufferForWrite<byte>(0, m_BufferChunkSize);
                var marker = MappedBuffer.PackMarker(0, m_BufferChunkSize);
                m_MappedBuffers[i] = new MappedBuffer
                {
                    m_Data = (byte*)data.GetUnsafePtr(),
                    m_Marker = marker,
                    m_BufferID = id,
                };
            }

            m_ThreadData->m_Buffers = (MappedBuffer*)m_MappedBuffers.GetUnsafePtr();
            m_ThreadData->m_NumBuffers = numBuffersNeeded;

            // TODO: set safety handle on thread data
            return new ThreadedBovineSparseUploader
            {
                m_Data = m_ThreadData
            };
        }

        private void DispatchUploads(int numOps, GraphicsBuffer graphicsBuffer)
        {
            for (int iOp = 0; iOp < numOps; iOp += k_MaxThreadGroupsPerDispatch)
            {
                int opsBegin = iOp;
                int opsEnd = math.min(opsBegin + k_MaxThreadGroupsPerDispatch, numOps);
                int numThreadGroups = opsEnd - opsBegin;

                m_SparseUploaderShader.SetBuffer(m_CopyKernelIndex, m_SrcBufferID, graphicsBuffer);
                m_SparseUploaderShader.SetBuffer(m_CopyKernelIndex, m_DstBufferID, m_DestinationBuffer);
                m_SparseUploaderShader.SetInt(m_OperationsBaseID, opsBegin);

                m_SparseUploaderShader.Dispatch(m_CopyKernelIndex, numThreadGroups, 1, 1);
            }
        }

        private void StepFrame()
        {
            // TODO: release safety handle of thread data
            m_ThreadData->m_Buffers = null;
            m_ThreadData->m_NumBuffers = 0;
            m_ThreadData->m_CurrBuffer = 0;
        }

        /// <summary>
        /// Ends an upload frame and dispatches any upload operations added to the passed in ThreadedBovineSparseUploader.
        /// </summary>
        /// <param name="tsu">The ThreadedBovineSparseUploader to consume and process upload dispatches for. You must have created this with a call to BovineSparseUploader.Begin.</param>
        public void EndAndCommit(ThreadedBovineSparseUploader tsu)
        {
            // Enforce that EndAndCommit is only called with a valid ThreadedBovineSparseUploader
            if (!tsu.IsValid)
            {
                Debug.LogError("Invalid ThreadedBovineSparseUploader passed to EndAndCommit");
                return;
            }

            int numBuffers = m_ThreadData->m_NumBuffers;

            // If there is no work for us to do, early out so we don't add empty entries into m_FrameData
            if (numBuffers == 0 && !m_MappedBuffers.IsCreated)
                return;

            var frameData = m_FreeFrameData.Count > 0 ? m_FreeFrameData.Pop() : new FrameData();
            for (int iBuf = 0; iBuf < numBuffers; ++iBuf)
            {
                var mappedBuffer = m_MappedBuffers[iBuf];
                MappedBuffer.UnpackMarker(mappedBuffer.m_Marker, out var operationOffset, out var dataOffset);
                var numOps = (int) (operationOffset / UnsafeUtility.SizeOf<Operation>());
                var graphicsBufferID = mappedBuffer.m_BufferID;
                var graphicsBuffer = m_UploadBufferPool.GetBufferFromId(graphicsBufferID);

                if (numOps > 0)
                {
                    graphicsBuffer.UnlockBufferAfterWrite<byte>(m_BufferChunkSize);

                    DispatchUploads(numOps, graphicsBuffer);

                    frameData.m_Buffers.Push(graphicsBufferID);
                }
                else
                {
                    graphicsBuffer.UnlockBufferAfterWrite<byte>(0);
                    m_UploadBufferPool.PutBufferId(graphicsBufferID);
                }
            }

            if (frameData.m_Buffers.Count > 0)
            {
                if (m_UseGraphicsFence)
                {
                    frameData.m_Fence = UnityEngine.Graphics.CreateGraphicsFence(
                        GraphicsFenceType.CPUSynchronisation, SynchronisationStageFlags.ComputeProcessing);
                }

                m_FrameData.Add(frameData);
            }
            else
            {
                m_FreeFrameData.Push(frameData);
            }

            if (m_MappedBuffers.IsCreated)
                m_MappedBuffers.Dispose();

            StepFrame();
        }

        /// <summary>
        /// Requests pruning of upload buffers. The actual release will happen in FrameCleanup.
        /// </summary>
        /// <param name="requestedMaxSizeRetainedInBytes">Maximum memory target to keep alive in upload buffer pool. Only buffers marked as free will be pruned, so the memory retained might be more than requested.</param>
        public void PruneUploadBufferPoolOnFrameCleanup(int requestedMaxSizeRetainedInBytes)
        {
            m_RequestedUploadBufferPoolMaxSizeBytes = requestedMaxSizeRetainedInBytes;
            m_PruneUploadBufferPool = true;
        }

        /// <summary>
        /// Cleans up internal data and recovers buffers into the free buffer pool.
        /// </summary>
        /// <remarks>
        /// It's best practice to call this once at the end of every frame.
        /// </remarks>
        public void FrameCleanup()
        {
            var numBuffers = m_ThreadData->m_NumBuffers;

            if (numBuffers > 0)
            {
                // These buffers were never used, so they get returned to the pool at once
                for (int iBuf = 0; iBuf < numBuffers; ++iBuf)
                {
                    var mappedBuffer = m_MappedBuffers[iBuf];
                    MappedBuffer.UnpackMarker(mappedBuffer.m_Marker, out var operationOffset, out var dataOffset);
                    var graphicsBufferID = mappedBuffer.m_BufferID;
                    var graphicsBuffer = m_UploadBufferPool.GetBufferFromId(graphicsBufferID);

                    graphicsBuffer.UnlockBufferAfterWrite<byte>(0);
                    m_UploadBufferPool.PutBufferId(graphicsBufferID);
                }

                m_MappedBuffers.Dispose();
            }

            if (m_PruneUploadBufferPool)
            {
                m_UploadBufferPool.PruneFreeBuffers(m_RequestedUploadBufferPoolMaxSizeBytes);
                m_PruneUploadBufferPool = false;
            }

            StepFrame();
        }

        /// <summary>
        /// Calculates statistics about the current and previous frame uploads.
        /// </summary>
        /// <returns>Returns a new statistics struct that contains information about the frame uploads.</returns>
        public BovineSparseUploaderStats ComputeStats()
        {
            var stats = default(BovineSparseUploaderStats);

            var totalUploadMemory = m_UploadBufferPool.TotalBufferSize;
            stats.BytesGPUMemoryUsed = totalUploadMemory;
            stats.BytesGPUMemoryUploadedCurr = m_CurrentFrameUploadSize;
            stats.BytesGPUMemoryUploadedMax = m_MaxUploadSize;

            return stats;
        }
    }
}
