namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public struct NativeKeyedMap<TValue>
        where TValue : unmanaged
    {
        private UnsafeKeyedMap<TValue> keyedMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private AtomicSafetyHandle m_Safety;

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Required by safety injection.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeKeyedMap<TValue>>();
#endif

        public NativeKeyedMap(int capacity, int maxKey, AllocatorManager.AllocatorHandle allocator)
        {
            this.keyedMapData = new UnsafeKeyedMap<TValue>(capacity, maxKey, allocator.Handle);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(this.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeKeyedMap<TValue>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        public bool IsCreated => this.keyedMapData.IsCreated;

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            get
            {
                this.CheckRead();
                return this.keyedMapData.Capacity;
            }

            set
            {
                this.CheckWrite();
                this.keyedMapData.Capacity = value;
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif
            this.keyedMapData.Dispose();
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new UnsafeKeyedMapDataDisposeJob
            {
                Data = new UnsafeKeyedMapDataDispose
                {
                    Buffer = this.keyedMapData.buffer,
                    AllocatorLabel = this.keyedMapData.allocator,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = this.m_Safety,
#endif
                },
            }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(this.m_Safety);
#endif
            this.keyedMapData.buffer = null;

            return jobHandle;
        }

        public void Clear()
        {
            this.CheckWrite();
            this.keyedMapData.Clear();
        }

        public void Add(int key, TValue item)
        {
            this.CheckWrite();
            this.keyedMapData.Add(key, item);
        }

        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            this.CheckRead();
            return this.keyedMapData.TryGetFirstValue(key, out item, out it);
        }

        public bool TryGetNextValue(out TValue item, ref UnsafeKeyedMapIterator it)
        {
            this.CheckRead();
            return this.keyedMapData.TryGetNextValue(out item, ref it);
        }

        public void SetLength(int length)
        {
            this.CheckWrite();
            this.keyedMapData.SetLength(length);
        }

        public void RecalculateBuckets()
        {
            this.CheckWrite();
            this.keyedMapData.RecalculateBuckets();
        }

        public unsafe int* GetUnsafeKeysPtr()
        {
            this.CheckWrite();
            return this.keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeValuesPtr()
        {
            this.CheckWrite();
            return this.keyedMapData.GetUnsafeValuesPtr();
        }

        public unsafe int* GetUnsafeReadOnlyKeysPtr()
        {
            this.CheckRead();
            return this.keyedMapData.GetUnsafeKeysPtr();
        }

        public unsafe TValue* GetUnsafeReadOnlyValuesPtr()
        {
            this.CheckRead();
            return this.keyedMapData.GetUnsafeValuesPtr();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
#endif
        }
    }
}
