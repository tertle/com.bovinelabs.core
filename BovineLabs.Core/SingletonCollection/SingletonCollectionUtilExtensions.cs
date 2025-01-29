// <copyright file="SingletonCollectionUtilExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SingletonCollection
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public static unsafe class SingletonCollectionUtilExtensions
    {
        /// <summary>
        /// Ensure a <see cref="NativeParallelHashMap{TKey,TValue}" /> has the capacity to be filled with all events of a specific type.
        /// If the hash map already has elements, it will increase the size so that all events and existing elements can fit.
        /// </summary>
        /// <param name="util"> The ISingletonCollectionUtil. </param>
        /// <param name="state"> The system state. </param>
        /// <param name="handle"> Input dependencies. </param>
        /// <param name="hashMap"> The <see cref="NativeHashMap{TKey,TValue}" /> to ensure capacity of. </param>
        /// <typeparam name="T"> The ISingletonCollectionUtil type. </typeparam>
        /// <typeparam name="TKey"> The key type of the <see cref="NativeHashMap{TKey,TValue}" /> . </typeparam>
        /// <typeparam name="TValue"> The value type of the <see cref="NativeHashMap{TKey,TValue}" /> . </typeparam>
        /// <returns> The dependency handle. </returns>
        public static JobHandle EnsureHashMapCapacity<T, TKey, TValue>(
            this T util, ref SystemState state, JobHandle handle, NativeParallelHashMap<TKey, TValue> hashMap)
            where T : unmanaged, ISingletonCollectionUtil<NativeThreadStream>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var streams = util.Containers;
            if (streams.Length != 0)
            {
                var counter = new NativeArray<int>(streams.Length, state.WorldUpdateAllocator);
                handle = new CountJob
                {
                    Counter = counter,
                    Streams = streams,
                }.ScheduleParallel(streams.Length, 1, handle);

                handle = new EnsureHashMapCapacityJob<TKey, TValue>
                {
                    Counter = counter,
                    HashMap = hashMap,
                }.Schedule(handle);

                handle = counter.Dispose(handle);
            }

            return handle;
        }

        /// <summary>
        /// Ensure a <see cref="NativeParallelMultiHashMap{TKey,TValue}" /> has the capacity to be filled with all events of a specific type.
        /// If the hash map already has elements, it will increase the size so that all events and existing elements can fit.
        /// </summary>
        /// <param name="util"> The ISingletonCollectionUtil. </param>
        /// <param name="state"> The system state. </param>
        /// <param name="handle"> Input dependencies. </param>
        /// <param name="hashMap"> The <see cref="NativeHashMap{TKey,TValue}" /> to ensure capacity of. </param>
        /// <typeparam name="T"> The ISingletonCollectionUtil type. </typeparam>
        /// <typeparam name="TKey"> The key type of the <see cref="NativeHashMap{TKey,TValue}" /> . </typeparam>
        /// <typeparam name="TValue"> The value type of the <see cref="NativeHashMap{TKey,TValue}" /> . </typeparam>
        /// <returns> The dependency handle. </returns>
        public static JobHandle EnsureHashMapCapacity<T, TKey, TValue>(
            this T util, ref SystemState state, JobHandle handle, NativeParallelMultiHashMap<TKey, TValue> hashMap)
            where T : unmanaged, ISingletonCollectionUtil<NativeThreadStream>
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var streams = util.Containers;
            if (streams.Length != 0)
            {
                var counter = new NativeArray<int>(streams.Length, state.WorldUpdateAllocator);
                handle = new CountJob
                {
                    Counter = counter,
                    Streams = streams,
                }.ScheduleParallel(streams.Length, 1, handle);

                handle = new EnsureMultiHashMapCapacityJob<TKey, TValue>
                {
                    Counter = counter,
                    HashMap = hashMap,
                }.Schedule(handle);

                handle = counter.Dispose(handle);
            }

            return handle;
        }

        [BurstCompile]
        private struct CountJob : IJobFor
        {
            public NativeArray<int> Counter;

            [ReadOnly]
            public UnsafeList<NativeThreadStream>.ReadOnly Streams;

            public void Execute(int index)
            {
                this.Counter[index] = this.Streams.Ptr[index].Count();
            }
        }

        [BurstCompile]
        private struct EnsureHashMapCapacityJob<TKey, TValue> : IJob
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            [ReadOnly]
            public NativeArray<int> Counter;

            public NativeParallelHashMap<TKey, TValue> HashMap;

            public void Execute()
            {
                var count = mathex.sum(this.Counter);
                var requiredSize = this.HashMap.Count() + count;

                if (this.HashMap.Capacity < requiredSize)
                {
                    this.HashMap.Capacity = requiredSize;
                }
            }
        }

        [BurstCompile]
        private struct EnsureMultiHashMapCapacityJob<TKey, TValue> : IJob
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            [ReadOnly]
            public NativeArray<int> Counter;

            public NativeParallelMultiHashMap<TKey, TValue> HashMap;

            public void Execute()
            {
                var count = mathex.sum(this.Counter);
                var requiredSize = this.HashMap.Count() + count;

                if (this.HashMap.Capacity < requiredSize)
                {
                    this.HashMap.Capacity = requiredSize;
                }
            }
        }
    }
}
