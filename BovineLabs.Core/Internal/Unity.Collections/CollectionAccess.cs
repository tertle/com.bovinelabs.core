namespace BovineLabs.Core.Internal
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public static unsafe class CollectionAccess
    {

        public static ref AllocatorManager.AllocatorHandle GetAllocator<T>(this ref UnsafeQueue<T> value)
            where T : unmanaged
        {
            return ref value.m_AllocatorLabel;
        }

        public static ref HashMapHelper<T> GetData<T>(this ref UnsafeHashSet<T> value)
            where T : unmanaged, IEquatable<T>
        {
            return ref UnsafeUtility.As<Unity.Collections.LowLevel.Unsafe.HashMapHelper<T>, HashMapHelper<T>>(ref value.m_Data);
        }

        public static ref UnsafeParallelHashMap<TKey, TValue> GetHashMapStorage<TKey, TValue>(this ref NativeParallelHashMap<TKey, TValue>.ReadOnly value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_HashMapData;
        }

        public static ref UnsafeParallelMultiHashMap<TKey, TValue> GetMultiHashMapStorage<TKey, TValue>(
            this ref NativeParallelMultiHashMap<TKey, TValue>.ReadOnly value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_MultiHashMapData;
        }

        public static HashMapHelper<TKey>* GetData<TKey, TValue>(this ref NativeHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (HashMapHelper<TKey>*)value.m_Data;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<TKey, TValue>(this ref NativeHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static ref UnsafeParallelHashMap<TKey, TValue> GetHashMapStorage<TKey, TValue>(this ref NativeParallelHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_HashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<TKey, TValue>(this ref NativeParallelHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static ref UnsafeParallelMultiHashMap<TKey, TValue> GetMultiHashMapStorage<TKey, TValue>(this ref NativeParallelMultiHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_MultiHashMapData;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<TKey, TValue>(this ref NativeParallelMultiHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static ref HashMapHelper<TKey> GetData<TKey, TValue>(this ref UnsafeHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref UnsafeUtility.As<Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>, HashMapHelper<TKey>>(ref value.m_Data);
        }

        public static UnsafeParallelHashMapData* GetBuffer<TKey, TValue>(this ref UnsafeParallelHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (UnsafeParallelHashMapData*)value.m_Buffer;
        }

        public static ref AllocatorManager.AllocatorHandle GetAllocator<TKey, TValue>(this ref UnsafeParallelHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_AllocatorLabel;
        }

        public static UnsafeParallelHashMapData* GetBuffer<TKey, TValue>(this ref UnsafeParallelHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (UnsafeParallelHashMapData*)value.m_Buffer;
        }

        public static ref int GetThreadIndex<TKey, TValue>(this ref UnsafeParallelHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_ThreadIndex;
        }

        public static UnsafeParallelHashMapData* GetBuffer<TKey, TValue>(this ref UnsafeParallelMultiHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (UnsafeParallelHashMapData*)value.m_Buffer;
        }

        public static ref AllocatorManager.AllocatorHandle GetAllocator<TKey, TValue>(this ref UnsafeParallelMultiHashMap<TKey, TValue> value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_AllocatorLabel;
        }

        public static UnsafeParallelHashMapData* GetBuffer<TKey, TValue>(this ref UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return (UnsafeParallelHashMapData*)value.m_Buffer;
        }

        public static ref UnsafeParallelHashMap<TKey, TValue>.ParallelWriter GetWriter<TKey, TValue>(
            this ref NativeParallelHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Writer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<TKey, TValue>(this ref NativeParallelHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static ref UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter GetWriter<TKey, TValue>(
            this ref NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Writer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<TKey, TValue>(this ref NativeParallelMultiHashMap<TKey, TValue>.ParallelWriter value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static HashMapHelper<T>* GetData<T>(this ref NativeHashSet<T> value)
            where T : unmanaged, IEquatable<T>
        {
            return (HashMapHelper<T>*)value.m_Data;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<T>(this ref NativeHashSet<T> value)
            where T : unmanaged, IEquatable<T>
        {
            return ref value.m_Safety;
        }
#endif

        public static ref NativeParallelHashMap<T, bool> GetData<T>(this ref NativeParallelHashSet<T> value)
            where T : unmanaged, IEquatable<T>
        {
            return ref value.m_Data;
        }

        public static ref NativeParallelHashMap<T, bool>.ParallelWriter GetData<T>(this ref NativeParallelHashSet<T>.ParallelWriter value)
            where T : unmanaged, IEquatable<T>
        {
            return ref value.m_Data;
        }

        public static ref UnsafeParallelHashMap<T, bool> GetData<T>(this ref UnsafeParallelHashSet<T> value)
            where T : unmanaged, IEquatable<T>
        {
            return ref value.m_Data;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static ref AtomicSafetyHandle GetSafety<T>(this ref NativeList<T> value)
            where T : unmanaged
        {
            return ref value.m_Safety;
        }
#endif

        public static UnsafeList<T>* GetListData<T>(this ref NativeList<T> value)
            where T : unmanaged
        {
            return (UnsafeList<T>*)value.m_ListData;
        }

        public static ref UnsafeQueue<T>.ParallelWriter GetWriter<T>(this ref NativeQueue<T>.ParallelWriter value)
            where T : unmanaged
        {
            return ref value.unsafeWriter;
        }

        public static void* GetBuffer<T>(this ref UnsafeQueue<T>.ParallelWriter value)
            where T : unmanaged
        {
            return (void*)value.m_Buffer;
        }

        public static ref int GetThreadIndex<T>(this ref UnsafeQueue<T>.ParallelWriter value)
            where T : unmanaged
        {
            return ref value.m_ThreadIndex;
        }
        public static int GetEntryIndex<TKey>(this NativeParallelMultiHashMapIterator<TKey> iterator)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return iterator.EntryIndex;
        }

        internal static NativeHashMap<TKey, TValue>.Enumerator CreateNativeEnumerator<TKey, TValue>(HashMapHelper<TKey>* data
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            , AtomicSafetyHandle safety
#endif
        )
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new NativeHashMap<TKey, TValue>.Enumerator
            {
                m_Enumerator = new Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.Enumerator(
                    (Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>*)data),
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Safety = safety,
#endif
            };
        }

        internal static UnsafeHashMap<TKey, TValue>.Enumerator CreateUnsafeEnumerator<TKey, TValue>(HashMapHelper<TKey>* data)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new UnsafeHashMap<TKey, TValue>.Enumerator
            {
                m_Enumerator = new Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>.Enumerator(
                    (Unity.Collections.LowLevel.Unsafe.HashMapHelper<TKey>*)data),
            };
        }

        internal static JobHandle ScheduleHashMapDispose<TKey>(HashMapHelper<TKey>* data, JobHandle dependency
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            , AtomicSafetyHandle safety
#endif
        )
            where TKey : unmanaged, IEquatable<TKey>
        {
            return new Unity.Collections.NativeHashMapDisposeJob
            {
                Data = new Unity.Collections.NativeHashMapDispose
                {
                    m_HashMapData = (UnsafeHashMap<int, int>*)data,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = safety,
#endif
                },
            }.Schedule(dependency);
        }

        internal static JobHandle ScheduleDispose(void* pointer, AllocatorManager.AllocatorHandle allocator, JobHandle dependency)
        {
            return new Unity.Collections.LowLevel.Unsafe.UnsafeDisposeJob
            {
                Ptr = pointer,
                Allocator = allocator,
            }.Schedule(dependency);
        }

    }
}
