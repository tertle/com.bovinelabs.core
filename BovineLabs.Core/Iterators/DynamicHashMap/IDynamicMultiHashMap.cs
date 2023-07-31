// <copyright file="IDynamicMultiHashMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Entities;

    public interface IDynamicMultiHashMap<TKey, TValue> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        byte Value { get; }
    }
}
