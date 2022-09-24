// <copyright file="NativeHashMapProxy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public readonly unsafe struct NativeHashMapProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        private readonly UnsafeParallelHashMap<TKey, TValue> hashmap;

        public NativeHashMapProxy(NativeParallelHashMap<TKey, TValue> nativeHashMap)
        {
            this.hashmap = nativeHashMap.GetReadOnlyUnsafeHashMap();
        }

        public NativeParallelHashMap<TKey, TValue> ToNativeHashMap(AtomicSafetyManager* safetyManager)
        {
            var nativeHashMap = this.hashmap.AsNative();

            safetyManager->MarkNativeHashMapAsReadOnly(ref nativeHashMap);
            return nativeHashMap;
        }
    }
}