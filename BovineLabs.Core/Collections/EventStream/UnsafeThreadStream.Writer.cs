namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe partial struct UnsafeThreadStream
    {
        public readonly struct Writer
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly UnsafeThreadStreamBlockData* _blockStream;

            internal Writer(ref UnsafeThreadStream stream)
            {
                _blockStream = stream._blockData;
            }

            public void Write<T>(T value)
                where T : struct
            {
                ref var dst = ref Allocate<T>();
                dst = value;
            }

            public ref T Allocate<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(Allocate(size));
            }

            public byte* Allocate(int size)
            {
                var threadIndex = JobsUtility.ThreadIndex;

                var ranges = _blockStream->Ranges + threadIndex;

                var ptr = ranges->CurrentPtr;
                var allocationEnd = ptr + size;

                ranges->CurrentPtr = allocationEnd;

                if (allocationEnd > ranges->CurrentBlockEnd)
                {
                    var oldBlock = ranges->CurrentBlock;
                    var newBlock = _blockStream->Allocate(oldBlock, threadIndex);

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

            public void WriteLarge<T>(NativeArray<T> array)
                where T : unmanaged
            {
                var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
                WriteLarge((byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
            }

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
                    var dst = Allocate(allocationRemainder * num);
                    UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var dst = Allocate(maxSize);
                    UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
                }
            }

            public void WriteLarge(byte* data, int size)
            {
                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var ptr = Allocate(allocationRemainder);
                    UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxLargeSize), allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = Allocate(MaxLargeSize);
                    UnsafeUtility.MemCpy(ptr, data + (i * MaxLargeSize), MaxLargeSize);
                }
            }
        }
    }
}
