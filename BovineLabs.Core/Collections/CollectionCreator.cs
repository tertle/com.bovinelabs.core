// <copyright file="CollectionCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class CollectionCreator
    {
        public static UnsafeHashMap<TKey, TValue>* CreateHashMap<TKey, TValue>(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashMap = AllocatorManager.Allocate<UnsafeHashMap<TKey, TValue>>(allocator);
            *hashMap = new UnsafeHashMap<TKey, TValue>(initialCapacity, allocator);
            return hashMap;
        }

        public static void Destroy<TKey, TValue>(UnsafeHashMap<TKey, TValue>* hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var allocator = hashMap->m_Data.Allocator;
            hashMap->Dispose();
            AllocatorManager.Free(allocator, hashMap);
        }

        public static UnsafeMultiHashMap<TKey, TValue>* CreateMultiHashMap<TKey, TValue>(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashMap = AllocatorManager.Allocate<UnsafeMultiHashMap<TKey, TValue>>(allocator);
            *hashMap = new UnsafeMultiHashMap<TKey, TValue>(initialCapacity, allocator);
            return hashMap;
        }

        public static void Destroy<TKey, TValue>(UnsafeMultiHashMap<TKey, TValue>* hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var allocator = hashMap->data.Allocator;
            hashMap->Dispose();
            AllocatorManager.Free(allocator, hashMap);
        }

        public static UnsafeHashSet<T>* CreateHashSet<T>(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged, IEquatable<T>
        {
            var hashSet = AllocatorManager.Allocate<UnsafeHashSet<T>>(allocator);
            *hashSet = new UnsafeHashSet<T>(initialCapacity, allocator);
            return hashSet;
        }

        public static void Destroy<T>(UnsafeHashSet<T>* hashMap)
            where T : unmanaged, IEquatable<T>
        {
            var allocator = hashMap->m_Data.Allocator;
            hashMap->Dispose();
            AllocatorManager.Free(allocator, hashMap);
        }

        public static UnsafeParallelHashMap<TKey, TValue>* CreateParallelHashMap<TKey, TValue>(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashMap = AllocatorManager.Allocate<UnsafeParallelHashMap<TKey, TValue>>(allocator);
            *hashMap = new UnsafeParallelHashMap<TKey, TValue>(initialCapacity, allocator);
            return hashMap;
        }

        public static void Destroy<TKey, TValue>(UnsafeParallelHashMap<TKey, TValue>* hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var allocator = hashMap->m_AllocatorLabel;
            hashMap->Dispose();
            AllocatorManager.Free(allocator, hashMap);
        }

        public static UnsafeParallelMultiHashMap<TKey, TValue>* CreateParallelMultiHashMap<TKey, TValue>(
            int initialCapacity, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashMap = AllocatorManager.Allocate<UnsafeParallelMultiHashMap<TKey, TValue>>(allocator);
            *hashMap = new UnsafeParallelMultiHashMap<TKey, TValue>(initialCapacity, allocator);
            return hashMap;
        }

        public static void Destroy<TKey, TValue>(UnsafeParallelMultiHashMap<TKey, TValue>* hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var allocator = hashMap->m_AllocatorLabel;
            hashMap->Dispose();
            AllocatorManager.Free(allocator, hashMap);
        }

        public static UnsafeQueue<T>* CreateQueue<T>(AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            var queue = UnsafeQueue<T>.Alloc(allocator);
            *queue = new UnsafeQueue<T>(allocator);
            return queue;
        }

        public static void Destroy<T>(UnsafeQueue<T>* queue)
            where T : unmanaged
        {
            UnsafeQueue<T>.Free(queue);
        }
    }
}
