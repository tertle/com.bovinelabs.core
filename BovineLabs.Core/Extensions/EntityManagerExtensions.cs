// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Extensions for <see cref="EntityManager" />. </summary>
    public static unsafe class EntityManagerExtensions
    {
        private const EntityQueryOptions QueryOptions = EntityQueryOptions.IncludeSystems;

        public static int NumberOfArchetype(this EntityManager entityManager)
        {
            return entityManager.GetCheckedEntityDataAccess()->EntityComponentStore->m_Archetypes.Length;
        }

        public static DynamicBuffer<T> GetChunkBuffer<T>(this EntityManager em, Entity entity)
            where T : unmanaged, IBufferElementData
        {
            var store = em.GetCheckedEntityDataAccess()->EntityComponentStore;
            store->AssertEntitiesExist(&entity, 1);
            var chunk = store->GetChunk(entity);
            return em.GetBuffer<T>(chunk.MetaChunkEntity);
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

        public static void* GetComponentDataRaw(this EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

            return access->GetComponentDataRawRW(entity, componentType.TypeIndex);
        }

        public static void AddSharedComponentRaw(this EntityManager entityManager, EntityQuery entityQuery, ComponentType componentType, void* componentData)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            access->AssertMainThread();
            access->AssertQueryIsValid(entityQuery);
            var queryImpl = entityQuery._GetImpl();
            if (queryImpl->IsEmptyIgnoreFilter)
            {
                return;
            }

            var size = TypeManager.GetTypeInfo(componentType.TypeIndex).ElementSize;
            var defaultValue = stackalloc byte[size];

            var changes = access->BeginStructuralChanges();
            var newSharedComponentDataIndex = access->InsertSharedComponent_Unmanaged(componentType.TypeIndex, 0, componentData, defaultValue);
            access->AddSharedComponentDataToQueryDuringStructuralChange_Unmanaged(queryImpl, newSharedComponentDataIndex, componentType, componentData);
            access->EndStructuralChanges(ref changes);
        }

        public static void AddSharedComponentManaged(
            this EntityManager entityManager, EntityQuery entityQuery, ComponentType componentType, object componentData)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            access->AssertQueryIsValid(entityQuery);
            var queryImpl = entityQuery._GetImpl();
            if (queryImpl->IsEmptyIgnoreFilter)
            {
                return;
            }

            var changes = access->BeginStructuralChanges();
            var newSharedComponentDataIndex = access->InsertSharedComponent_Managed(componentType.TypeIndex, 0, componentData);
            access->AddSharedComponentDataToQueryDuringStructuralChange(queryImpl, newSharedComponentDataIndex, componentType);

            access->EndStructuralChanges(ref changes);
        }

        public static void* GetSharedComponentRaw(this EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var sharedComponentIndex = access->EntityComponentStore->GetSharedComponentDataIndex(entity, componentType.TypeIndex);

            return access->EntityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, componentType.TypeIndex);
        }

        public static object GetSharedComponentManagedBoxed(this EntityManager entityManager, Entity entity, ComponentType componentType)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var sharedComponentIndex = access->EntityComponentStore->GetSharedComponentDataIndex(entity, componentType.TypeIndex);

            return access->ManagedComponentStore.GetSharedComponentDataBoxed(sharedComponentIndex, componentType.TypeIndex);
        }

        /// <summary> Gets or creates the <see cref="T" /> singleton entity. </summary>
        /// <param name="em"> The entity manager. </param>
        /// <typeparam name="T"> The singleton type. </typeparam>
        /// <returns> The entity. </returns>
        public static Entity GetOrCreateSingletonEntity<T>(this EntityManager em)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (!query.IsEmptyIgnoreFilter)
            {
                return query.GetSingletonEntity();
            }

            var entity = em.CreateEntity();
            em.AddComponent<T>(entity);

            return entity;
        }

        public static Entity GetSingletonEntity<T>(this EntityManager em)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            query.CompleteDependency();

            return query.GetSingletonEntity();
        }

        public static bool TryGetSingletonEntity<T>(this EntityManager em, out Entity entity)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            if (query.CalculateEntityCount() != 1)
            {
                entity = Entity.Null;

                return false;
            }

            entity = query.GetSingletonEntity();

            return true;
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

        public static void SetSingleton<T>(this EntityManager em, T value, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            query.SetSingleton(value);
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

        public static T GetSingletonObject<T>(this EntityManager em, bool completeDependency = true)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return em.GetComponentObject<T>(query.GetSingletonEntity());
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

        public static bool TryGetSingletonBuffer<T>(this EntityManager em, out DynamicBuffer<T> buffer, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            if (query.CalculateEntityCount() != 1)
            {
                buffer = default;

                return false;
            }

            buffer = query.GetSingletonBuffer<T>(isReadOnly);

            return true;
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public static T GetManagedSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : class
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static bool TryGetManagedSingleton<T>(this EntityManager em, out T component, bool completeDependency = true)
            where T : class
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);

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
