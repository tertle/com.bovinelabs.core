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
                m_BlockStream = stream._blockData;
                m_CurrentBlock = null;
                m_CurrentPtr = null;
                m_CurrentBlockEnd = null;
                m_RemainingItemCount = 0;
                m_LastBlockSize = 0;
            }

            /// <summary>
            /// BeginForEachIndex and EndForEachIndex must be balanced; ending verifies that all iteration data was read.
            /// </summary>
            public int BeginForEachIndex(int foreachIndex)
            {
                m_RemainingItemCount = m_BlockStream->Ranges[foreachIndex].ElementCount;
                m_LastBlockSize = m_BlockStream->Ranges[foreachIndex].LastOffset;

                m_CurrentBlock = m_BlockStream->Ranges[foreachIndex].Block;
                m_CurrentPtr = (byte*)m_CurrentBlock + m_BlockStream->Ranges[foreachIndex].OffsetInFirstBlock;
                m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;

                return m_RemainingItemCount;
            }

            /// <summary>
            /// BeginForEachIndex and EndForEachIndex must be balanced; ending verifies that all iteration data was read.
            /// </summary>
            public void EndForEachIndex()
            {
            }

            public int ForEachCount => UnsafeThreadStream.ForEachCount;

            public int RemainingItemCount => m_RemainingItemCount;

            public byte* ReadUnsafePtr(int size)
            {
                m_RemainingItemCount--;

                var ptr = m_CurrentPtr;
                m_CurrentPtr += size;

                if (m_CurrentPtr > m_CurrentBlockEnd)
                {
                    m_CurrentBlock = m_CurrentBlock->Next;
                    m_CurrentPtr = m_CurrentBlock->Data;

                    m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;

                    ptr = m_CurrentPtr;
                    m_CurrentPtr += size;
                }

                return ptr;
            }

            public ref T Read<T>()
                where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            public ref T Peek<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();

                var ptr = m_CurrentPtr;
                if (ptr + size > m_CurrentBlockEnd)
                {
                    ptr = m_CurrentBlock->Next->Data;
                }

                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            public int Count()
            {
                var itemCount = 0;
                for (var i = 0; i != UnsafeThreadStream.ForEachCount; i++)
                {
                    itemCount += m_BlockStream->Ranges[i].ElementCount;
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
                    var ptr = ReadUnsafePtr(allocationRemainder);
                    UnsafeUtility.MemCpy(buffer + (allocationCount * MaxLargeSize), ptr, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = ReadUnsafePtr(MaxLargeSize);
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
                    var ptr = ReadUnsafePtr(allocationRemainder);
                    UnsafeUtility.MemCpy(buffer + (allocationCount * MaxLargeSize), ptr, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = ReadUnsafePtr(MaxLargeSize);
                    UnsafeUtility.MemCpy(buffer + (i * MaxLargeSize), ptr, MaxLargeSize);
                }
            }
        }
    }
}
