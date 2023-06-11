// <copyright file="IDynamicIndexMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Needed for safety.")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Defines memory layout")]
    public interface IDynamicIndexMap<TValue> : IBufferElementData
        where TValue : unmanaged
    {
        byte Value { get; }
    }
}
