// <copyright file="IDynamicIndexedMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Needed for safety.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Defines memory layout")]
    public interface IDynamicIndexedMap<TKey, TIndex, TValue> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        byte Value { get; }
    }
}
