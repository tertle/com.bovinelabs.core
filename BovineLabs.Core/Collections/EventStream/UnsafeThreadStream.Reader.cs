namespace BovineLabs.Core.Collections
{
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct UnsafeThreadStream
    {
        public struct Reader : INativeStreamReader
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeThreadStreamBlockData* m_BlockStream;

            [NativeDisableUnsafePtrRestriction]
            internal UnsafeThreadStreamBlock* m_CurrentBlock;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentPtr;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentBlockEnd;

            internal int m_RemainingItemCount;
            internal int m_LastBlockSize;

            internal Reader(ref UnsafeThreadStream stream)
            {
                this.m_BlockStream = stream.blockData;
                this.m_CurrentBlock = null;
                this.m_CurrentPtr = null;
                this.m_CurrentBlockEnd = null;
                this.m_RemainingItemCount = 0;
                this.m_LastBlockSize = 0;
            }

            /// <summary>
            /// BeginForEachIndex and EndForEachIndex must be balanced; ending verifies that all iteration data was read.
            /// </summary>
            public int BeginForEachIndex(int foreachIndex)
            {
                this.m_RemainingItemCount = this.m_BlockStream->Ranges[foreachIndex].ElementCount;
                this.m_LastBlockSize = this.m_BlockStream->Ranges[foreachIndex].LastOffset;

                this.m_CurrentBlock = this.m_BlockStream->Ranges[foreachIndex].Block;
                this.m_CurrentPtr = (byte*)this.m_CurrentBlock + this.m_BlockStream->Ranges[foreachIndex].OffsetInFirstBlock;
                this.m_CurrentBlockEnd = (byte*)this.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;

                return this.m_RemainingItemCount;
            }

            /// <summary>
            /// BeginForEachIndex and EndForEachIndex must be balanced; ending verifies that all iteration data was read.
            /// </summary>
            public void EndForEachIndex()
            {
            }

            public int ForEachCount => UnsafeThreadStream.ForEachCount;

            public int RemainingItemCount => this.m_RemainingItemCount;

            public byte* ReadUnsafePtr(int size)
            {
                this.m_RemainingItemCount--;

                var ptr = this.m_CurrentPtr;
                this.m_CurrentPtr += size;

                if (this.m_CurrentPtr > this.m_CurrentBlockEnd)
                {
                    this.m_CurrentBlock = this.m_CurrentBlock->Next;
                    this.m_CurrentPtr = this.m_CurrentBlock->Data;

                    this.m_CurrentBlockEnd = (byte*)this.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;

                    ptr = this.m_CurrentPtr;
                    this.m_CurrentPtr += size;
                }

                return ptr;
            }

            public ref T Read<T>()
                where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.ReadUnsafePtr(size));
            }

            public ref T Peek<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();

                var ptr = this.m_CurrentPtr;
                if (ptr + size > this.m_CurrentBlockEnd)
                {
                    ptr = this.m_CurrentBlock->Next->Data;
                }

                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            public int Count()
            {
                var itemCount = 0;
                for (var i = 0; i != UnsafeThreadStream.ForEachCount; i++)
                {
                    itemCount += this.m_BlockStream->Ranges[i].ElementCount;
                }

                return itemCount;
            }

            public void ReadLarge(byte* buffer, int size)
            {
                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra chunk allocation most times
                if (allocationRemainder > 0)
                {
                    var ptr = this.ReadUnsafePtr(allocationRemainder);
                    UnsafeUtility.MemCpy(buffer + (allocationCount * MaxLargeSize), ptr, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = this.ReadUnsafePtr(MaxLargeSize);
                    UnsafeUtility.MemCpy(buffer + (i * MaxLargeSize), ptr, MaxLargeSize);
                }
            }

            public void ReadLarge<T>(byte* buffer, int length)
                where T : unmanaged
            {
                var size = sizeof(T) * length;

                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra chunk allocation most times
                if (allocationRemainder > 0)
                {
                    var ptr = this.ReadUnsafePtr(allocationRemainder);
                    UnsafeUtility.MemCpy(buffer + (allocationCount * MaxLargeSize), ptr, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = this.ReadUnsafePtr(MaxLargeSize);
                    UnsafeUtility.MemCpy(buffer + (i * MaxLargeSize), ptr, MaxLargeSize);
                }
            }
        }
    }
}
