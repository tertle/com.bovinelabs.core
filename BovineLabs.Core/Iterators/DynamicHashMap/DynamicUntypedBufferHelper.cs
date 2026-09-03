// <copyright file="DynamicUntypedBufferHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DynamicUntypedBufferHelper
    {
        internal int OffsetsOffset;
        internal int SizesOffset;
        internal int TypesOffset;
        internal int AlignmentsOffset;
        internal int DataOffset;
        internal int Count;
        internal int Capacity;
        internal int DataCapacity;
        internal int DataAllocatedIndex;
        internal int Log2MinGrowth;

        internal byte* Data
        {
            get
            {
                fixed (DynamicUntypedBufferHelper* data = &this)
                {
                    return (byte*)data + data->DataOffset;
                }
            }
        }

        internal int* Offsets
        {
            get
            {
                fixed (DynamicUntypedBufferHelper* data = &this)
                {
                    return (int*)((byte*)data + data->OffsetsOffset);
                }
            }
        }

        internal int* Sizes
        {
            get
            {
                fixed (DynamicUntypedBufferHelper* data = &this)
                {
                    return (int*)((byte*)data + data->SizesOffset);
                }
            }
        }

        internal int* Types
        {
            get
            {
                fixed (DynamicUntypedBufferHelper* data = &this)
                {
                    return (int*)((byte*)data + data->TypesOffset);
                }
            }
        }

        internal byte* Alignments
        {
            get
            {
                fixed (DynamicUntypedBufferHelper* data = &this)
                {
                    return (byte*)data + data->AlignmentsOffset;
                }
            }
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int dataCapacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);
            dataCapacity = CalcCapacityCeilPow2(0, dataCapacity, log2MinGrowth);

            var totalSize = CalculateDataSize(
                capacity,
                dataCapacity,
                out var offsetsOffset,
                out var sizesOffset,
                out var typesOffset,
                out var alignmentsOffset,
                out var dataOffset);

            var bufferDataSize = sizeof(DynamicUntypedBufferHelper);
            buffer.ResizeUninitialized(bufferDataSize + totalSize);

            var data = buffer.AsUntypedBufferHelper();

            data->Count = 0;
            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->DataCapacity = dataCapacity;
            data->DataAllocatedIndex = 0;

            data->OffsetsOffset = bufferDataSize + offsetsOffset;
            data->SizesOffset = bufferDataSize + sizesOffset;
            data->TypesOffset = bufferDataSize + typesOffset;
            data->AlignmentsOffset = bufferDataSize + alignmentsOffset;
            data->DataOffset = bufferDataSize + dataOffset;
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicUntypedBufferHelper* data, int newCapacity)
        {
            if (newCapacity <= data->Capacity)
            {
                return;
            }

            Assert.IsTrue(newCapacity > data->Capacity);

            var totalSize = CalculateDataSize(
                newCapacity,
                data->DataCapacity,
                out var offsetsOffset,
                out var sizesOffset,
                out var typesOffset,
                out var alignmentsOffset,
                out var dataOffset);

            var oldCapacity = data->Capacity;
            var oldCount = data->Count;
            var oldDataCapacity = data->DataCapacity;
            var oldDataAllocatedIndex = data->DataAllocatedIndex;
            var oldLog2MinGrowth = data->Log2MinGrowth;

            var oldOffsets = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldSizes = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldTypes = (int*)UnsafeUtility.Malloc(oldCapacity * sizeof(int), UnsafeUtility.AlignOf<int>(), Allocator.Temp);
            var oldAlignments = (byte*)UnsafeUtility.Malloc(oldCapacity * sizeof(byte), UnsafeUtility.AlignOf<byte>(), Allocator.Temp);
            var oldData = (byte*)UnsafeUtility.Malloc(oldDataAllocatedIndex, UnsafeUtility.AlignOf<byte>(), Allocator.Temp);

            if (oldCapacity > 0)
            {
                UnsafeUtility.MemCpy(oldOffsets, data->Offsets, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(oldSizes, data->Sizes, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(oldTypes, data->Types, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(oldAlignments, data->Alignments, oldCapacity * sizeof(byte));
            }

            if (oldDataAllocatedIndex > 0)
            {
                UnsafeUtility.MemCpy(oldData, data->Data, oldDataAllocatedIndex);
            }

            var bufferDataSize = sizeof(DynamicUntypedBufferHelper);
            buffer.ResizeUninitialized(bufferDataSize + totalSize);

            data = buffer.AsUntypedBufferHelper();
            data->Count = oldCount;
            data->Capacity = newCapacity;
            data->DataCapacity = oldDataCapacity;
            data->DataAllocatedIndex = oldDataAllocatedIndex;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->OffsetsOffset = bufferDataSize + offsetsOffset;
            data->SizesOffset = bufferDataSize + sizesOffset;
            data->TypesOffset = bufferDataSize + typesOffset;
            data->AlignmentsOffset = bufferDataSize + alignmentsOffset;
            data->DataOffset = bufferDataSize + dataOffset;

            if (oldCapacity > 0)
            {
                UnsafeUtility.MemCpy(data->Offsets, oldOffsets, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(data->Sizes, oldSizes, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(data->Types, oldTypes, oldCapacity * sizeof(int));
                UnsafeUtility.MemCpy(data->Alignments, oldAlignments, oldCapacity * sizeof(byte));
            }

            if (oldDataAllocatedIndex > 0)
            {
                UnsafeUtility.MemCpy(data->Data, oldData, oldDataAllocatedIndex);
            }
        }

        internal static void ResizeData(DynamicBuffer<byte> buffer, ref DynamicUntypedBufferHelper* data, int newCapacity)
        {
            if (newCapacity <= data->DataCapacity)
            {
                return;
            }

            var toAllocate = newCapacity - data->DataCapacity;
            var newBufferCapacity = buffer.Length + toAllocate;

            buffer.ResizeUninitialized(newBufferCapacity);
            data = buffer.AsUntypedBufferHelper();

            data->DataCapacity = newCapacity;
        }

        internal void Clear()
        {
            this.Count = 0;
            this.DataAllocatedIndex = 0;
        }

        internal static int Add<TValue>(DynamicBuffer<byte> buffer, ref DynamicUntypedBufferHelper* data, TValue value)
            where TValue : unmanaged
        {
            if (data->Count == data->Capacity)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            var idx = data->Count++;
            data->CheckIndexOutOfBounds(idx);

            var size = sizeof(TValue);
            var align = UnsafeUtility.AlignOf<TValue>();
            Check.Assume((align & (align - 1)) == 0, "Alignment must be power-of-two.");
            Check.Assume(align <= byte.MaxValue, "Alignment exceeds byte storage.");
            var dataAllocatedIndex = AlignDataIndex(data, data->DataAllocatedIndex, align);
            var minNewCapacity = dataAllocatedIndex + size;

            if (minNewCapacity > data->DataCapacity)
            {
                var newCap = data->DataCapacity;
                do
                {
                    newCap = CalcCapacityCeilPow2(newCap + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                }
                while (newCap < minNewCapacity);

                ResizeData(buffer, ref data, newCap);
            }

            var dst = data->Data + dataAllocatedIndex;
            UnsafeUtility.MemCpy(dst, &value, size);

            data->Offsets[idx] = dataAllocatedIndex;
            data->Sizes[idx] = size;
            data->Types[idx] = BurstRuntime.GetHashCode32<TValue>();
            data->Alignments[idx] = (byte)align;
            data->DataAllocatedIndex = dataAllocatedIndex + size;

            return idx;
        }

        internal static ref TValue GetValue<TValue>(DynamicUntypedBufferHelper* data, int index)
            where TValue : unmanaged
        {
            data->CheckIndexInRange(index);
            data->CheckType<TValue>(index);

            var offset = data->Offsets[index];
            return ref UnsafeUtility.AsRef<TValue>(data->Data + offset);
        }

        internal static void SetValue<TValue>(DynamicUntypedBufferHelper* data, int index, TValue value)
            where TValue : unmanaged
        {
            data->CheckIndexInRange(index);
            data->CheckType<TValue>(index);

            var offset = data->Offsets[index];
            UnsafeUtility.MemCpy(data->Data + offset, &value, sizeof(TValue));
        }

        internal static void RemoveAt(ref DynamicUntypedBufferHelper* data, int index)
        {
            data->CheckIndexInRange(index);

            var count = data->Count;
            if (count == 0)
            {
                return;
            }

            var offsets = data->Offsets;
            var sizes = data->Sizes;
            var types = data->Types;
            var alignments = data->Alignments;

            var moveCount = count - index - 1;
            if (moveCount > 0)
            {
                UnsafeUtility.MemMove(offsets + index, offsets + index + 1, moveCount * sizeof(int));
                UnsafeUtility.MemMove(sizes + index, sizes + index + 1, moveCount * sizeof(int));
                UnsafeUtility.MemMove(types + index, types + index + 1, moveCount * sizeof(int));
                UnsafeUtility.MemMove(alignments + index, alignments + index + 1, moveCount * sizeof(byte));
            }

            data->Count = count - 1;
            if (data->Count == 0)
            {
                data->DataAllocatedIndex = 0;
                return;
            }

            var dataIndex = 0;
            for (var i = 0; i < data->Count; ++i)
            {
                dataIndex = AlignDataIndex(data, dataIndex, alignments[i]);

                var size = sizes[i];
                var oldOffset = offsets[i];
                if (oldOffset != dataIndex)
                {
                    UnsafeUtility.MemMove(data->Data + dataIndex, data->Data + oldOffset, size);
                }

                offsets[i] = dataIndex;
                dataIndex += size;
            }

            data->DataAllocatedIndex = dataIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacityCeilPow2(int count, int capacity, int log2MinGrowth)
        {
            capacity = math.max(math.max(1, count), capacity);
            var newCapacity = math.max(capacity, 1 << log2MinGrowth);
            var result = math.ceilpow2(newCapacity);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacityCeilPow2(int capacity, int log2MinGrowth)
        {
            var newCapacity = math.max(capacity, 1 << log2MinGrowth);
            var result = math.ceilpow2(newCapacity);

            return result;
        }

        private static int CalculateDataSize(
            int capacity, int dataCapacity, out int offsetsOffset, out int sizesOffset, out int typesOffset, out int alignmentsOffset, out int dataOffset)
        {
            var sizeOfInt = sizeof(int);
            var offsetsSize = sizeOfInt * capacity;
            var sizesSize = sizeOfInt * capacity;
            var typesSize = sizeOfInt * capacity;
            var alignmentsSize = sizeof(byte) * capacity;

            offsetsOffset = 0;
            sizesOffset = CollectionHelper.Align(offsetsOffset + offsetsSize, sizeOfInt);
            typesOffset = CollectionHelper.Align(sizesOffset + sizesSize, sizeOfInt);
            alignmentsOffset = CollectionHelper.Align(typesOffset + typesSize, sizeOfInt);
            dataOffset = CollectionHelper.Align(alignmentsOffset + alignmentsSize, 16);

            return dataOffset + dataCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AlignDataIndex(DynamicUntypedBufferHelper* data, int dataIndex, int align)
        {
            if (align <= 1)
            {
                return dataIndex;
            }

            // Alignment must be applied against absolute addresses, not just offsets.
            var dataMisalignment = (int)((ulong)data->Data & (ulong)(align - 1));
            if (dataMisalignment == 0)
            {
                return CollectionHelper.Align(dataIndex, align);
            }

            return CollectionHelper.Align(dataIndex + dataMisalignment, align) - dataMisalignment;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)this.Capacity)
            {
                throw new InvalidOperationException($"Internal buffer error. idx {idx}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexInRange(int idx)
        {
            if ((uint)idx >= (uint)this.Count)
            {
                throw new IndexOutOfRangeException($"Index {idx} is out of range in DynamicUntypedBuffer of '{this.Count}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckType<TValue>(int idx)
            where TValue : unmanaged
        {
            var expected = BurstRuntime.GetHashCode32<TValue>();
            var actual = UnsafeUtility.ReadArrayElement<int>(this.Types, idx);
            if (!expected.Equals(actual))
            {
                throw new InvalidOperationException($"Type {actual} does not match stored {expected}");
            }

            var size = UnsafeUtility.ReadArrayElement<int>(this.Sizes, idx);
            if (size != sizeof(TValue))
            {
                throw new InvalidOperationException($"Size {size} does not match stored {sizeof(TValue)}");
            }
        }
    }
}
