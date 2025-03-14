// <copyright file="SystemStateExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public static class SystemStateExtensions
    {
        private const EntityQueryOptions QueryOptions = EntityQueryOptions.IncludeSystems;

        public static JobHandle GetInternalDependency(ref this SystemState system)
        {
            return system.m_JobHandle;
        }

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

        public static UnsafeEntityDataAccess GetUnsafeEntityDataAccess(ref this SystemState system)
        {
            return system.EntityManager.GetUnsafeEntityDataAccess();
        }

        public static UnsafeComponentLookup<T> GetUnsafeComponentLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetUnsafeComponentLookup<T>(isReadOnly);
        }

        public static UnsafeBufferLookup<T> GetUnsafeBufferLookup<T>(ref this SystemState system, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            system.AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return system.EntityManager.GetUnsafeBufferLookup<T>(isReadOnly);
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

        public static Entity GetSingletonEntity<T>(ref this SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);
            query.CompleteDependency();
            return query.GetSingletonEntity();
        }

        public static bool TryGetSingletonEntity<T>(ref this SystemState state, out Entity entity)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);

            if (query.CalculateChunkCount() != 1)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            return true;
        }

        public static bool HasSingleton<T>(ref this SystemState state)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);
            return query.CalculateChunkCount() == 1;
        }

        public static T GetSingleton<T>(ref this SystemState state, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static void SetSingleton<T>(ref this SystemState state, T value, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(ref state);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            query.SetSingleton(value);
        }

        public static RefRW<T> GetSingletonRW<T>(ref this SystemState state, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(ref state);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonRW<T>();
        }

        public static bool TryGetSingleton<T>(ref this SystemState state, out T component, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);

            if (query.CalculateEntityCount() != 1)
            {
                component = default;
                return false;
            }

            if (completeDependency)
            {
                query.CompleteDependency();
            }

            component = query.GetSingleton<T>();
            return true;
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(ref this SystemState state, bool isReadOnly = false, bool completeDependency = true)
            where T : unmanaged, IBufferElementData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonBuffer<T>(isReadOnly);
        }

        public static bool TryGetSingletonBuffer<T>(
            ref this SystemState state, out DynamicBuffer<T> buffer, bool isReadOnly = false, bool completeDependency = true)
            where T : unmanaged, IBufferElementData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(ref state);

            if (query.CalculateEntityCount() != 1)
            {
                buffer = default;
                return false;
            }

            if (completeDependency)
            {
                query.CompleteDependency();
            }

            buffer = query.GetSingletonBuffer<T>(isReadOnly);
            return true;
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public static T GetManagedSingleton<T>(ref this SystemState state, bool completeDependency = true)
            where T : class, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(ref state);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static bool TryGetManagedSingleton<T>(ref this SystemState state, out T component, bool completeDependency = true)
            where T : class, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(ref state);

            if (query.CalculateEntityCount() != 1)
            {
                component = default;
                return false;
            }

            if (completeDependency)
            {
                query.CompleteDependency();
            }

            component = query.GetSingleton<T>();
            return true;
        }
#endif

        public static unsafe JobHandle GetAllSystemDependencies(ref this SystemState state)
        {
            var jobHandles = new NativeList<JobHandle>(state.WorldUpdateAllocator);

            using var e = state.WorldUnmanaged.GetImpl().m_SystemStatePtrMap.GetEnumerator();
            while (e.MoveNext())
            {
                var systemState = (SystemState*)e.Current.Value;
                jobHandles.Add(systemState->m_JobHandle);
            }

            // This seems slightly faster than doing JobHandle.CompleteAll()
            return JobHandle.CombineDependencies(jobHandles.AsArray());
        }
    }
}
