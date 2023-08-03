// <copyright file="UnsafeThreadStream.Reader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct UnsafeThreadStream
    {
        /// <summary>
        /// </summary>
        [GenerateTestsForBurstCompatibility]
        public struct Reader
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

            /// <summary> Begin reading data at the iteration index. </summary>
            /// <param name="foreachIndex"> </param>
            /// <remarks> BeginForEachIndex must always be called balanced by a EndForEachIndex. </remarks>
            /// <returns> The number of elements at this index. </returns>
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
            /// Ensures that all data has been read for the active iteration index.
            /// </summary>
            /// <remarks> EndForEachIndex must always be called balanced by a BeginForEachIndex. </remarks>
            public void EndForEachIndex()
            {
            }

            /// <summary>
            /// Returns for each count.
            /// </summary>
            public int ForEachCount => UnsafeThreadStream.ForEachCount;

            /// <summary>
            /// Returns remaining item count.
            /// </summary>
            public int RemainingItemCount => this.m_RemainingItemCount;

            /// <summary>
            /// Returns pointer to data.
            /// </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to data. </returns>
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

            /// <summary>
            /// Read data.
            /// </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to data. </returns>
            [GenerateTestsForBurstCompatibility]
            public ref T Read<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.ReadUnsafePtr(size));
            }

            /// <summary>
            /// Peek into data.
            /// </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to data. </returns>
            [GenerateTestsForBurstCompatibility]
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

            /// <summary>
            /// The current number of items in the container.
            /// </summary>
            /// <returns> The item count. </returns>
            public int Count()
            {
                var itemCount = 0;
                for (var i = 0; i != UnsafeThreadStream.ForEachCount; i++)
                {
                    itemCount += this.m_BlockStream->Ranges[i].ElementCount;
                }

                return itemCount;
            }
        }
    }
}
