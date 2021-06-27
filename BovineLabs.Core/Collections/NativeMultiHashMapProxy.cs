// <copyright file="NativeMultiHashMapProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe struct NativeMultiHashMapProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        private readonly UnsafeMultiHashMap<TKey, TValue> hashmap;

        public NativeMultiHashMapProxy(NativeMultiHashMap<TKey, TValue> nativeHashMap)
        {
            this.hashmap = nativeHashMap.GetHashMapData();
        }

        public NativeMultiHashMap<TKey, TValue> ToNativeHashMap(AtomicSafetyManager* safetyManager)
        {
            var nativeHashMap = this.hashmap.AsNative();
            safetyManager->MarkNativeHashMapAsReadOnly(ref nativeHashMap);
            return nativeHashMap;
        }
    }
}