namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Internal;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct NativeThreadStream
    {
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Reader : INativeStreamReader
        {
            private UnsafeThreadStream.Reader _reader;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private int _remainingBlocks;
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            internal AtomicSafetyHandle m_Safety;
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Reader>();
#endif

            internal Reader(ref NativeThreadStream stream)
            {
                _reader = stream._stream.AsReader();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _remainingBlocks = 0;
                m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "BovineLabs.Core.Collections.NativeThreadStream.Reader");
#endif
            }

            public int ForEachCount => _reader.ForEachCount;

            public int RemainingItemCount => CollectionChecks.AssumePositive(_reader.RemainingItemCount);

            public int BeginForEachIndex(int foreachIndex)
            {
                CheckBeginForEachIndex(foreachIndex);

                var remainingItemCount = _reader.BeginForEachIndex(foreachIndex);
                remainingItemCount = CollectionChecks.AssumePositive(remainingItemCount);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                _remainingBlocks = _reader.m_BlockStream->Ranges[foreachIndex].NumberOfBlocks;
                if (_remainingBlocks == 0)
                {
                    _reader.m_CurrentBlockEnd = (byte*)_reader.m_CurrentBlock + _reader.m_LastBlockSize;
                }
#endif

                return remainingItemCount;
            }

            /// <summary>
            /// BeginForEachIndex and EndForEachIndex must be balanced; ending verifies that all iteration data was read.
            /// </summary>
            public void EndForEachIndex()
            {
                _reader.EndForEachIndex();
                CheckEndForEachIndex();
            }

            public byte* ReadUnsafePtr(int size)
            {
                CheckReadSize(size);

                _reader.m_RemainingItemCount--;

                var ptr = _reader.m_CurrentPtr;
                _reader.m_CurrentPtr += size;

                if (_reader.m_CurrentPtr > _reader.m_CurrentBlockEnd)
                {
                    _reader.m_CurrentBlock = _reader.m_CurrentBlock->Next;
                    _reader.m_CurrentPtr = _reader.m_CurrentBlock->Data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    _remainingBlocks--;

                    CheckNotReadingOutOfBounds(size);

                    if (_remainingBlocks <= 0)
                    {
                        _reader.m_CurrentBlockEnd = (byte*)_reader.m_CurrentBlock + _reader.m_LastBlockSize;
                    }
                    else
                    {
                        _reader.m_CurrentBlockEnd = (byte*)_reader.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;
                    }
#else
                    this.reader.m_CurrentBlockEnd = (byte*)this.reader.m_CurrentBlock + UnsafeThreadStreamBlockData.AllocationSize;
#endif
                    ptr = _reader.m_CurrentPtr;
                    _reader.m_CurrentPtr += size;
                }

                return ptr;
            }

            public ref T Read<T>()
                where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            public int Count()
            {
                CheckRead();
                return _reader.Count();
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

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckNotReadingOutOfBounds(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (_remainingBlocks < 0)
                {
                    throw new ArgumentException("Reading out of bounds");
                }

                if (_remainingBlocks == 0 && size + sizeof(void*) > _reader.m_LastBlockSize)
                {
                    throw new ArgumentException("Reading out of bounds");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckRead()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckReadSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                Assert.IsTrue(size <= UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*));
                if (_reader.m_RemainingItemCount < 1)
                {
                    throw new ArgumentException("There are no more items left to be read.");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckBeginForEachIndex(int forEachIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                if ((uint)forEachIndex >= (uint)ForEachCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(forEachIndex),
                        $"foreachIndex: {forEachIndex} must be between 0 and ForEachCount: {ForEachCount}");
                }
#endif
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckEndForEachIndex()
            {
                if (_reader.m_RemainingItemCount != 0)
                {
                    throw new ArgumentException("Not all elements (Count) have been read. If this is intentional, simply skip calling EndForEachIndex();");
                }

                if (_reader.m_CurrentBlockEnd != _reader.m_CurrentPtr)
                {
                    throw new ArgumentException("Not all data (Data Size) has been read. If this is intentional, simply skip calling EndForEachIndex();");
                }
            }
        }
    }
}
