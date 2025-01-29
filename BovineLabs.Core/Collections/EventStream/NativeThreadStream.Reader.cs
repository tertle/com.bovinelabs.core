// <copyright file="NativeThreadStream.Reader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct NativeThreadStream
    {
        /// <summary> The reader instance. </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Reader
        {
            private UnsafeThreadStream.Reader reader;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private int remainingBlocks;
#pragma warning disable SA1308
            private readonly AtomicSafetyHandle m_Safety;
#pragma warning restore SA1308
#endif

            internal Reader(ref NativeThreadStream stream)
            {
                this.reader = stream.stream.AsReader();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.remainingBlocks = 0;
                this.m_Safety = stream.m_Safety;
#endif
            }

            /// <summary> Gets the for each count. </summary>
            public int ForEachCount => this.reader.ForEachCount;

            /// <summary> Gets the remaining item count. </summary>
            public int RemainingItemCount => CollectionHelper.AssumePositive(this.reader.RemainingItemCount);

            /// <summary> Begin reading data at the iteration index. </summary>
            /// <param name="foreachIndex"> The index to start reading. </param>
            /// <returns> The number of elements at this index. </returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                this.CheckBeginForEachIndex(foreachIndex);

                var remainingItemCount = this.reader.BeginForEachIndex(foreachIndex);
                remainingItemCount = CollectionHelper.AssumePositive(remainingItemCount);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.remainingBlocks = this.reader.m_BlockStream->Ranges[foreachIndex].NumberOfBlocks;
                if (this.remainingBlocks == 0)
                {
                    this.reader.m_CurrentBlockEnd = (byte*)this.reader.m_CurrentBlock + this.reader.m_LastBlockSize;
                }
#endif

                return remainingItemCount;
            }

            /// <summary> Ensures that all data has been read for the active iteration index. </summary>
            /// <remarks> EndForEachIndex must always be called balanced by a BeginForEachIndex. </remarks>
            public void EndForEachIndex()
            {
                this.reader.EndForEachIndex();
                this.CheckEndForEachIndex();
            }

            /// <summary> Returns pointer to data. </summary>
            /// <param name="size"> The size of the data to read. </param>
            /// <returns> The pointer to the data. </returns>
            public byte* ReadUnsafePtr(int size)
            {
                this.CheckReadSize(size);

                this.reader.m_RemainingItemCount--;

                var ptr = this.reader.m_CurrentPtr;
                this.reader.m_CurrentPtr += size;

                if (this.reader.m_CurrentPtr > this.reader.m_CurrentBlockEnd)
                {
                    this.reader.m_CurrentBlock = this.reader.m_CurrentBlock->Next;
                    this.reader.m_CurrentPtr = this.reader.m_CurrentBlock->Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    this.remainingBlocks--;

                    this.CheckNotReadingOutOfBounds(size);

                    if (this.remainingBlocks <= 0)
                    {
                        this.reader.m_CurrentBlockEnd = (byte*)this.reader.m_CurrentBlock + this.reader.m_LastBlockSize;
                    }
                    else
                    {
                        this.reader.m_CurrentBlockEnd = (byte*)this.reader.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;
                    }
#else
                    this.reader.m_CurrentBlockEnd = (byte*)this.reader.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;
#endif
                    ptr = this.reader.m_CurrentPtr;
                    this.reader.m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary> Read data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> The returned data. </returns>
            public ref T Read<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.ReadUnsafePtr(size));
            }

            /// <summary>
            /// The current number of items in the container.
            /// </summary>
            /// <returns> The item count. </returns>
            public int Count()
            {
                this.CheckRead();
                return this.reader.Count();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckNotReadingOutOfBounds(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (this.remainingBlocks < 0)
                {
                    throw new ArgumentException("Reading out of bounds");
                }

                if (this.remainingBlocks == 0 && size + sizeof(void*) > this.reader.m_LastBlockSize)
                {
                    throw new ArgumentException("Reading out of bounds");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckReadSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);

                Assert.IsTrue(size <= UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*));
                if (this.reader.m_RemainingItemCount < 1)
                {
                    throw new ArgumentException("There are no more items left to be read.");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckBeginForEachIndex(int forEachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);

                if ((uint)forEachIndex >= (uint)this.ForEachCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(forEachIndex),
                        $"foreachIndex: {forEachIndex} must be between 0 and ForEachCount: {this.ForEachCount}");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckEndForEachIndex()
            {
                if (this.reader.m_RemainingItemCount != 0)
                {
                    throw new ArgumentException("Not all elements (Count) have been read. If this is intentional, simply skip calling EndForEachIndex();");
                }

                if (this.reader.m_CurrentBlockEnd != this.reader.m_CurrentPtr)
                {
                    throw new ArgumentException("Not all data (Data Size) has been read. If this is intentional, simply skip calling EndForEachIndex();");
                }
            }
        }
    }
}
