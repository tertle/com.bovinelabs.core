// <copyright file="UnsafeThreadStream.Writer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe partial struct UnsafeThreadStream
    {
        /// <summary> The writer instance. </summary>
        public readonly struct Writer
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly UnsafeThreadStreamBlockData* blockStream;

            internal Writer(ref UnsafeThreadStream stream)
            {
                this.blockStream = stream.blockData;
            }

            /// <summary> Write data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <param name="value"> Value to write. </param>
            public void Write<T>(T value)
                where T : struct
            {
                ref var dst = ref this.Allocate<T>();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to allocated space for data. </returns>
            public ref T Allocate<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to allocated space for data. </returns>
            public byte* Allocate(int size)
            {
                var threadIndex = JobsUtility.ThreadIndex;

                var ranges = this.blockStream->Ranges + threadIndex;

                var ptr = ranges->CurrentPtr;
                var allocationEnd = ptr + size;

                ranges->CurrentPtr = allocationEnd;

                if (allocationEnd > ranges->CurrentBlockEnd)
                {
                    var oldBlock = ranges->CurrentBlock;
                    var newBlock = this.blockStream->Allocate(oldBlock, threadIndex);

                    ranges->CurrentBlock = newBlock;
                    ranges->CurrentPtr = newBlock->Data;

                    if (ranges->Block == null)
                    {
                        ranges->OffsetInFirstBlock = (int)(newBlock->Data - (byte*)newBlock);
                        ranges->Block = newBlock;
                    }
                    else
                    {
                        ranges->NumberOfBlocks++;
                    }

                    ranges->CurrentBlockEnd = (byte*)newBlock + UnsafeThreadStreamBlockData.AllocationSize;

                    ptr = newBlock->Data;
                    ranges->CurrentPtr = newBlock->Data + size;
                }

                ranges->ElementCount++;
                ranges->LastOffset = (int)(ranges->CurrentPtr - (byte*)ranges->CurrentBlock);

                return ptr;
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="array"> The array to write. </param>
            /// <typeparam name="T"> The type of the array. </typeparam>
            public void WriteLarge<T>(NativeArray<T> array)
                where T : unmanaged
            {
                var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
                this.WriteLarge((byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="data"> The data to write. </param>
            /// <typeparam name="T"> The type of the slice. </typeparam>
            public void WriteLarge<T>(NativeSlice<T> data)
                where T : unmanaged
            {
                var num = UnsafeUtility.SizeOf<T>();
                var countPerAllocate = MaxLargeSize / num;

                var allocationCount = data.Length / countPerAllocate;
                var allocationRemainder = data.Length % countPerAllocate;

                var maxSize = countPerAllocate * num;
                var maxOffset = data.Stride * countPerAllocate;

                var src = (byte*)data.GetUnsafeReadOnlyPtr();

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var dst = this.Allocate(allocationRemainder * num);
                    UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var dst = this.Allocate(maxSize);
                    UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
                }
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="data"> The data to write. </param>
            /// <param name="size"> The size of the data. For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
            public void WriteLarge(byte* data, int size)
            {
                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var ptr = this.Allocate(allocationRemainder);
                    UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxLargeSize), allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = this.Allocate(MaxLargeSize);
                    UnsafeUtility.MemCpy(ptr, data + (i * MaxLargeSize), MaxLargeSize);
                }
            }
        }
    }
}
