// <copyright file="ComponentSystemBaseExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    public static class ComponentSystemBaseExtensions
    {
        public static unsafe SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this ComponentSystemBase system, bool isReadOnly = false)
            where T : struct, ISharedComponentData
        {
            return system.CheckedState()->GetSharedComponentDataFromIndex<T>(isReadOnly);
        }

        public static unsafe SharedComponentLookup<T> GetSharedComponentLookup<T>(this ComponentSystemBase system, bool isReadOnly = false)
            where T : unmanaged, ISharedComponentData
        {
            return system.CheckedState()->GetSharedComponentLookup<T>(isReadOnly);
        }
    }
}
