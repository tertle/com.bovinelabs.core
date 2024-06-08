// <copyright file="UnsafeParallelMultiHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeParallelMultiHashMapExtensions
    {
        public static UnsafeParallelHashMapBucketData GetBucketData<TKey, TValue>(this in UnsafeParallelMultiHashMap<TKey, TValue> hashMap)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.m_Buffer->GetBucketData();
        }
    }
}
