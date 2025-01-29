// <copyright file="SingletonCollectionExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SingletonCollection
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Collections;

    public static class SingletonCollectionExtensions
    {
        public static unsafe NativeThreadStream.Writer CreateThreadStream<TS>(this TS eventSingleton)
            where TS : unmanaged, ISingletonCollection<NativeThreadStream>
        {
            var stream = new NativeThreadStream(eventSingleton.Allocator);
            eventSingleton.Collections->Add(stream);
            return stream.AsWriter();
        }

        public static unsafe NativeArray<TC> CreateArray<TS, TC>(
            this TS eventSingleton, int length, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where TS : unmanaged, ISingletonCollection<NativeArray<TC>>
            where TC : unmanaged
        {
            var collection = new NativeArray<TC>(length, eventSingleton.Allocator, options);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeList<TC> CreateList<TS, TC>(this TS eventSingleton, int capacity)
            where TS : unmanaged, ISingletonCollection<NativeList<TC>>
            where TC : unmanaged
        {
            var collection = new NativeList<TC>(capacity, eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeQueue<TC> CreateQueue<TS, TC>(this TS eventSingleton)
            where TS : unmanaged, ISingletonCollection<NativeQueue<TC>>
            where TC : unmanaged
        {
            var collection = new NativeQueue<TC>(eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeHashMap<TKey, TValue> CreateHashMap<TS, TKey, TValue>(this TS eventSingleton, int capacity)
            where TS : unmanaged, ISingletonCollection<NativeHashMap<TKey, TValue>>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var collection = new NativeHashMap<TKey, TValue>(capacity, eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeMultiHashMap<TKey, TValue> CreateMultiHashMap<TS, TKey, TValue>(this TS eventSingleton, int capacity)
            where TS : unmanaged, ISingletonCollection<NativeMultiHashMap<TKey, TValue>>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var collection = new NativeMultiHashMap<TKey, TValue>(capacity, eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeParallelHashMap<TKey, TValue> CreateParallelHashMap<TS, TKey, TValue>(this TS eventSingleton, int capacity)
            where TS : unmanaged, ISingletonCollection<NativeParallelHashMap<TKey, TValue>>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var collection = new NativeParallelHashMap<TKey, TValue>(capacity, eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }

        public static unsafe NativeParallelMultiHashMap<TKey, TValue> CreateParallelMultiHashMap<TS, TKey, TValue>(this TS eventSingleton, int capacity)
            where TS : unmanaged, ISingletonCollection<NativeParallelMultiHashMap<TKey, TValue>>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var collection = new NativeParallelMultiHashMap<TKey, TValue>(capacity, eventSingleton.Allocator);
            eventSingleton.Collections->Add(collection);
            return collection;
        }
    }
}
