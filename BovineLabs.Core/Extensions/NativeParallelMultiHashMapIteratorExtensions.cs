// <copyright file="NativeParallelMultiHashMapIteratorExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using Unity.Collections;

    public static class NativeParallelMultiHashMapIteratorExtensions
    {
        public static int EntryIndex<TKey>(this in NativeParallelMultiHashMapIterator<TKey> iterator)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return iterator.EntryIndex;
        }
    }
}
