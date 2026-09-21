namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    public unsafe struct NativePerfectHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged, IEquatable<TValue>
    {
        [NativeDisableUnsafePtrRestriction]
        private UnsafePerfectHashMap<TKey, TValue>* _data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        internal AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<UnsafePerfectHashMap<TKey, TValue>>();
#endif

        public NativePerfectHashMap(NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue, AllocatorManager.AllocatorHandle allocator)
        {
            _data = UnsafePerfectHashMap<TKey, TValue>.Alloc(keys, values, nullValue, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativePerfectHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data != null;
        }

        public TValue this[TKey key]
        {
            get
            {
                CheckRead();
                return (*_data)[key];
            }

            set
            {
                CheckWrite();
                (*_data)[key] = value;
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
#endif

            if (!IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif

            UnsafePerfectHashMap<TKey, TValue>.Free(_data);
            _data = null;
        }

        public bool TryGetValue(TKey key, out TValue item)
        {
            CheckRead();
            return _data->TryGetValue(key, out item);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }
    }
}
