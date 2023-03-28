// <copyright file="KVPairExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections;

    public static class KVPairExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KVPair<TKey, TValue> source, out TKey key, out TValue val)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            key = source.Key;
            val = source.Value;
        }
    }
}
