// <copyright file="SystemStateExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Iterators;
    using Unity.Entities;
    using Unity.Jobs;

    public static class SystemStateExtensions
    {
        public static JobHandle GetInternalDependency(ref this SystemState system)
        {
            return system.m_JobHandle;
        }

        public static SharedComponentLookup<T> GetSharedComponentLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, ISharedComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetSharedComponentLookup<T>(isReadOnly);
        }

        public static UnsafeEntityDataAccess GetUnsafeEntityDataAccess(ref this SystemState system)
        {
            return system.EntityManager.GetUnsafeEntityDataAccess();
        }

        /// <summary>
        /// Get an <see cref="UnsafeEnableableLookup" />.
        /// All components that use this must manually add a dependency to the system for safety.
        /// </summary>
        /// <param name="system"> The system owner. </param>
        /// <returns> An <see cref="UnsafeEnableableLookup" />. </returns>
        public static UnsafeEnableableLookup GetUnsafeEnableableLookup(ref this SystemState system)
        {
            return system.EntityManager.GetUnsafeEnableableLookup();
        }

        public static ChangeFilterLookup<T> GetChangeFilterLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetChangeFilterLookup<T>(isReadOnly);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDependency(ref this SystemState state, TypeIndex typeIndex, bool isReadOnly = false)
        {
            state.AddDependency(isReadOnly ? ComponentType.ReadOnly(typeIndex) : ComponentType.ReadWrite(typeIndex));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDependency(ref this SystemState state, ComponentType componentType)
        {
            state.AddReaderWriter(componentType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddDependency<T>(ref this SystemState state, bool isReadOnly = false)
        {
            state.AddDependency(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
        }
    }
}
