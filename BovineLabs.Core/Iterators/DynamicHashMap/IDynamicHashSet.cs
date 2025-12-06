// <copyright file="IDynamicHashSet.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;

    [SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Needed for safety.")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Defines memory layout")]
    public interface IDynamicHashSet<TKey> : IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
    {
        byte Value { get; }
    }

    // public static class Extensions
    // {
    //     public static Stub AsMap<T>(this DynamicBuffer<T> map)
    //         where T : unmanaged, IBufferElementData
    //     {
    //         return default;
    //     }
    //
    //     public struct Stub
    //     {
    //         public bool Contains(object t)
    //         {
    //             return false;
    //         }
    //     }
    // }
}
