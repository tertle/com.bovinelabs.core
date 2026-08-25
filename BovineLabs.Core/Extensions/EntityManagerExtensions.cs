// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Extensions for <see cref="EntityManager" />. </summary>
    public static unsafe class EntityManagerExtensions
    {
        private const EntityQueryOptions QueryOptions = EntityQueryOptions.IncludeSystems;

        public static Entity GetEntityFromIndex(this EntityManager entityManager, int index)
        {
            return index is < 0 or >= EntityComponentStore.EntityStore.MaximumTheoreticalAmountOfEntities
                ? Entity.Null
                : entityManager.GetEntityByEntityIndex(index);
        }

        public static int NumberOfArchetype(this EntityManager entityManager)
        {
            return entityManager.GetCheckedEntityDataAccess()->EntityComponentStore->m_Archetypes.Length;
        }

        // Only use these for tests
        public static ComponentLookup<T> GetComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            return entityManager.GetComponentLookup<T>(isReadOnly);
        }

        // Only use these for tests
        public static BufferLookup<T> GetBufferLookup<T>(this EntityManager entityManager, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            return entityManager.GetBufferLookup<T>(isReadOnly);
        }

        public static UntypedDynamicBuffer GetUntypedBuffer(
            this EntityManager entityManager, Entity entity, ComponentType componentType, bool isReadOnly = false)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &access->DependencyManager->Safety;
#endif

            var typeIndex = componentType.TypeIndex;

            return access->GetUntypedBuffer(componentType, entity,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                safetyHandles->GetSafetyHandle(typeIndex, isReadOnly), safetyHandles->GetBufferSafetyHandle(typeIndex),
#endif
                isReadOnly);
        }

        public static bool HasSingleton<T>(this EntityManager em)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            return query.CalculateEntityCount() == 1;
        }

        public static T GetSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static RefRW<T> GetSingletonRW<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonRW<T>();
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

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

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            return query.GetSingletonBuffer<T>(isReadOnly);
        }

        public static DynamicBuffer<T> GetSingletonBufferNoSync<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            return query.GetSingletonBufferNoSync<T>(isReadOnly);
        }

        public static Entity CreateEntity<T>(this EntityManager em, T component, FixedString64Bytes name)
            where T : unmanaged, IComponentData
        {
            var e = em.CreateEntity<T>(name);
            em.SetComponentData(e, component);
            return e;
        }

        public static Entity CreateEntity<T>(this EntityManager em, FixedString64Bytes name)
            where T : unmanaged
        {
            Span<ComponentType> s = stackalloc ComponentType[1];
            s[0] = ComponentType.ReadWrite<T>();

            var e = em.CreateEntity(s);
            em.SetName(e, name);
            return e;
        }

        public static Entity CreateEntity<T1, T2>(this EntityManager em, FixedString64Bytes name)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            Span<ComponentType> s = stackalloc ComponentType[2];
            s[0] = ComponentType.ReadWrite<T1>();
            s[1] = ComponentType.ReadWrite<T2>();

            var e = em.CreateEntity(s);
            em.SetName(e, name);
            return e;
        }

        internal static SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this EntityManager entityManager, bool isReadOnly = true)
            where T : struct, ISharedComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new SharedComponentDataFromIndex<T>(typeIndex, access, isReadOnly);
#else
            return new SharedComponentDataFromIndex<T>(typeIndex, access);
#endif
        }

        internal static SharedComponentLookup<T> GetSharedComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = true)
            where T : unmanaged, ISharedComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new SharedComponentLookup<T>(typeIndex, access, isReadOnly);
#else
            return new SharedComponentLookup<T>(typeIndex, access);
#endif
        }

        internal static UnsafeEntityDataAccess GetUnsafeEntityDataAccess(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeEntityDataAccess(access);
        }

        internal static UnsafeEnableableLookup GetUnsafeEnableableLookup(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeEnableableLookup(access);
        }

        internal static UnsafeComponentLookup<T> GetUnsafeComponentLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new UnsafeComponentLookup<T>(typeIndex, access, isReadOnly);
#else
            return new UnsafeComponentLookup<T>(typeIndex, access);
#endif
        }

        internal static UnsafeBufferLookup<T> GetUnsafeBufferLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeBufferLookup<T>(typeIndex, access, isReadOnly);
        }

        // Internal because this is not safe called directly form EntityManager
        internal static ChangeFilterLookup<T> GetChangeFilterLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ChangeFilterLookup<T>(typeIndex, access, isReadOnly);
#else
            return new ChangeFilterLookup<T>(typeIndex, access);
#endif
        }
    }
}
