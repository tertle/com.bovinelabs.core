// <copyright file="ContainerJobExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Extensions
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public static class ContainerJobExtensions
    {
        public static JobHandle Clear<T>(this NativeList<T> list, JobHandle handle)
            where T : struct
        {
            return new ClearListJob<T> { List = list }.Schedule(handle);
        }

        public static JobHandle Clear<T>(this NativeHashSet<T> hashSet, JobHandle handle)
            where T : unmanaged, IEquatable<T>
        {
            return new ClearHashSetJob<T> { HashSet = hashSet }.Schedule(handle);
        }

        public static JobHandle Clear<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, JobHandle handle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new ClearNativeHashMapJob<TKey, TValue> { HashMap = hashMap }.Schedule(handle);
        }

        public static JobHandle Clear<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap, JobHandle handle)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            return new ClearNativeMultiHashMapJob<TKey, TValue> { HashMap = hashMap }.Schedule(handle);
        }

        [BurstCompile]
        private struct ClearListJob<T> : IJob
            where T : struct
        {
            public NativeList<T> List;

            public void Execute()
            {
                this.List.Clear();
            }
        }

        [BurstCompile]
        private struct ClearHashSetJob<T> : IJob
            where T : unmanaged, IEquatable<T>
        {
            public NativeHashSet<T> HashSet;

            public void Execute()
            {
                this.HashSet.Clear();
            }
        }

        [BurstCompile]
        private struct ClearNativeHashMapJob<TKey, TValue> : IJob
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            public NativeHashMap<TKey, TValue> HashMap;

            public void Execute()
            {
                this.HashMap.Clear();
            }
        }

        [BurstCompile]
        private struct ClearNativeMultiHashMapJob<TKey, TValue> : IJob
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            public NativeMultiHashMap<TKey, TValue> HashMap;

            public void Execute()
            {
                this.HashMap.Clear();
            }
        }
    }
}