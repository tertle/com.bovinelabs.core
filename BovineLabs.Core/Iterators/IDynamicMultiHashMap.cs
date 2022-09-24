// <copyright file="IDynamicMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;

    public interface IDynamicMultiHashMap<TKey, TValue> : IDynamicHashMapBase<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
    }
}
