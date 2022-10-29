// <copyright file="ComponentSystemBaseExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Iterators;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class ComponentSystemBaseExtensions
    {
        public static unsafe SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this ComponentSystemBase system, bool isReadOnly = false)
            where T : struct, ISharedComponentData
        {
            return system.CheckedState()->GetSharedComponentDataFromIndex<T>(isReadOnly);
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this ComponentSystemBase system, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            return system.EntityManager.GetBuffer<T>(system.GetSingletonEntity<T>(), isReadOnly);
        }

        public static bool TryGetSingletonBuffer<T>(this ComponentSystemBase system, out DynamicBuffer<T> buffer, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            if (!system.TryGetSingletonEntity<T>(out var entity))
            {
                buffer = default;
                return false;
            }

            buffer = system.EntityManager.GetBuffer<T>(entity, isReadOnly);
            return true;
        }
    }
}
