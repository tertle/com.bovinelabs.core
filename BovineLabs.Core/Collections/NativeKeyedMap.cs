// <copyright file="NativeKeyedMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

        /// <summary>
        /// Returns a newly allocated multi hash map.
        /// </summary>
        /// <param name="capacity"> The number of key-value pairs that should fit in the initial allocation. </param>
        /// <param name="maxKey"> Max value stored in this map. </param>
        /// <param name="allocator"> The allocator to use. </param>
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

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public bool IsCreated => this.keyedMapData.IsCreated;

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value> The number of key-value pairs that fit in the current allocation. </value>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        /// <exception cref="Exception"> Thrown if `value` is less than the current capacity. </exception>
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

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif
            this.keyedMapData.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps"> A job handle. The newly scheduled job will depend upon this handle. </param>
        /// <returns> The handle of a new job that will dispose this hash map. </returns>
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

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public void Clear()
        {
            this.CheckWrite();
            this.keyedMapData.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>
        /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
        /// </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="item"> The value to add. </param>
        public void Add(int key, TValue item)
        {
            this.CheckWrite();
            this.keyedMapData.Add(key, item);
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key"> The key. </param>
        /// <param name="item"> Outputs the associated value represented by the iterator. </param>
        /// <param name="it"> Outputs an iterator. </param>
        /// <returns> True if the key was present. </returns>
        public bool TryGetFirstValue(int key, out TValue item, out UnsafeKeyedMapIterator it)
        {
            this.CheckRead();
            return this.keyedMapData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item"> Outputs the next value. </param>
        /// <param name="it"> A reference to the iterator to advance. </param>
        /// <returns> True if the key was present and had another value. </returns>
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
