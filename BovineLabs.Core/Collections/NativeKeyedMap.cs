namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public struct NativeKeyedMap<TValue>
        where TValue : unmanaged
    {
        private UnsafeKeyedMap<TValue> _keyedMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeKeyedMap<TValue>>();
#endif

        public NativeKeyedMap(int capacity, int maxKey, AllocatorManager.AllocatorHandle allocator)
        {
            _keyedMapData = new UnsafeKeyedMap<TValue>(capacity, maxKey, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeKeyedMap<TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public bool IsCreated => _keyedMapData.IsCreated;

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            get
            {
                CheckRead();
                return _keyedMapData.Capacity;
            }

            set
            {
                CheckWrite();
                _keyedMapData.Capacity = value;
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _keyedMapData.Dispose();
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeKeyedMapDataDisposeJob
            {
                Data = new UnsafeKeyedMapDataDispose
                {
                    Buffer = _keyedMapData.buffer,
                    AllocatorLabel = _keyedMapData.allocator,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety,
#endif
                },
            }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif
            _keyedMapData.buffer = null;

            return jobHandle;
        }

        public void Clear()
        {
            CheckWrite();
            _keyedMapData.Clear();
        }

        public void Add(int key, TValue item)
        {
            CheckWrite();
            _keyedMapData.Add(key, item);
        }

        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            CheckRead();
            return _keyedMapData.TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            CheckRead();
            return _keyedMapData.TryGetNextValue(out item, ref it);
        }

        public void SetLength(int length)
        {
            CheckWrite();
            _keyedMapData.SetLength(length);
        }

        public void RecalculateBuckets()
        {
            CheckWrite();
            _keyedMapData.RecalculateBuckets();
        }

        public unsafe int* GetUnsafeKeysPtr()
        {
            CheckWrite();
            return _keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeValuesPtr()
        {
            CheckWrite();
            return _keyedMapData.GetUnsafeValuesPtr();
        }

        public unsafe int* GetUnsafeReadOnlyKeysPtr()
        {
            CheckRead();
            return _keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeReadOnlyValuesPtr()
        {
            CheckRead();
            return _keyedMapData.GetUnsafeValuesPtr();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }
    }
}
