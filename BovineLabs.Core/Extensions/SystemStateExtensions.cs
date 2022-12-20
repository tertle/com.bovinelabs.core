// <copyright file="SystemStateExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    public static class SystemStateExtensions
    {
        public static SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(ref this SystemState system, bool isReadOnly = false)
            where T : struct, ISharedComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetSharedComponentDataFromIndex<T>(isReadOnly);
        }

        public static SharedComponentLookup<T> GetSharedComponentLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, ISharedComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetSharedComponentLookup<T>(isReadOnly);
        }

        public static UnsafeEnableableLookup GetUnsafeEnableableLookup(ref this SystemState system)
        {
            return system.EntityManager.GetUnsafeEnableableLookup();
        }

        public static void AddSystemDependency(ref this SystemState state, TypeIndex typeIndex, bool isReadOnly = false)
        {
            state.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly(typeIndex) : ComponentType.ReadWrite(typeIndex));
        }
    }
}
