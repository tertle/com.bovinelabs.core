// <copyright file="ContainerClearJobs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    [BurstCompile]
    public struct ClearListJob<T> : IJob
        where T : unmanaged
    {
        public NativeList<T> List;

        public void Execute()
        {
            this.List.Clear();
        }
    }

    [BurstCompile]
    public struct ClearParallelHashSetJob<T> : IJob
        where T : unmanaged, IEquatable<T>
    {
        public NativeParallelHashSet<T> HashSet;

        public void Execute()
        {
            this.HashSet.Clear();
        }
    }

    [BurstCompile]
    public struct ClearHashSetJob<T> : IJob
        where T : unmanaged, IEquatable<T>
    {
        public NativeHashSet<T> HashSet;

        public void Execute()
        {
            this.HashSet.Clear();
        }
    }

    [BurstCompile]
    public struct ClearNativeHashMapJob<TKey, TValue> : IJob
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeHashMap<TKey, TValue> HashMap;

        public void Execute()
        {
            this.HashMap.Clear();
        }
    }

    [BurstCompile]
    public struct ClearNativeParallelHashMapJob<TKey, TValue> : IJob
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeParallelHashMap<TKey, TValue> HashMap;

        public void Execute()
        {
            this.HashMap.Clear();
        }
    }

    [BurstCompile]
    public struct ClearNativeParallelMultiHashMapJob<TKey, TValue> : IJob
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public NativeParallelMultiHashMap<TKey, TValue> HashMap;

        public void Execute()
        {
            this.HashMap.Clear();
        }
    }
}
