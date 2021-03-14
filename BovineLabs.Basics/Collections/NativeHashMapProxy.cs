// <copyright file="NativeHashMapProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Collections
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe struct NativeHashMapProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        private readonly UnsafeHashMap<TKey, TValue> hashmap;

        public NativeHashMapProxy(NativeHashMap<TKey, TValue> nativeHashMap)
        {
            this.hashmap = nativeHashMap.m_HashMapData;
        }

        public NativeHashMap<TKey, TValue> ToNativeHashMap(AtomicSafetyManager* safetyManager)
        {
            var nativeHashMap = new NativeHashMap<TKey, TValue> { m_HashMapData = this.hashmap };
            safetyManager->MarkNativeHashMapAsReadOnly(ref nativeHashMap);
            return nativeHashMap;
        }
    }
}