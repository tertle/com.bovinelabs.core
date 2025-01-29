// <copyright file="NativePerfectHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
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
        private UnsafePerfectHashMap<TKey, TValue>* data;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<UnsafePerfectHashMap<TKey, TValue>>();
#endif

        public NativePerfectHashMap(NativeArray<TKey> keys, NativeArray<TValue> values, TValue nullValue, AllocatorManager.AllocatorHandle allocator)
        {
            this.data = UnsafePerfectHashMap<TKey, TValue>.Alloc(keys, values, nullValue, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
            {
                AtomicSafetyHandle.SetNestedContainer(this.m_Safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativePerfectHashMap<TKey, TValue>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.data != null;
        }

        /// <summary> Gets and sets values by key. </summary>
        /// <remarks> Getting a key that is not present will throw. Setting a key that is not already present will add the key. </remarks>
        /// <param name="key"> The key to look up. </param>
        /// <value> The value associated with the key. </value>
        /// <exception cref="ArgumentException"> For getting, thrown if the key was not present. </exception>
        public TValue this[TKey key]
        {
            get
            {
                this.CheckRead();
                return (*this.data)[key];
            }

            set
            {
                this.CheckWrite();
                (*this.data)[key] = value;
            }
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif

            if (!this.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif

            UnsafePerfectHashMap<TKey, TValue>.Free(this.data);
            this.data = null;
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            this.CheckRead();
            return this.data->TryGetValue(key, out item);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(this.m_Safety);
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
