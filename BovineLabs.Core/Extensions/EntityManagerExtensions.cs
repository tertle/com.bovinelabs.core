// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
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

        public static SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this EntityManager entityManager, bool isReadOnly = true)
            where T : struct, ISharedComponentData
        {
            EntityDataAccess* access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new SharedComponentDataFromIndex<T>(typeIndex, access, isReadOnly);
#else
            return new SharedComponentDataFromIndex<T>(typeIndex, access);
#endif
        }

        public static SharedComponentLookup<T> GetSharedComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = true)
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

        internal static UnsafeEnableableLookup GetUnsafeEnableableLookup(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            return new UnsafeEnableableLookup(access);
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

        /// <summary> Gets or creates the <see cref="T" /> singleton entity. </summary>
        /// <param name="em"> The entity manager. </param>
        /// <typeparam name="T"> The singleton type. </typeparam>
        /// <returns> The entity. </returns>
        public static Entity GetOrCreateSingletonEntity<T>(this EntityManager em)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
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
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            query.CompleteDependency();
            return query.GetSingletonEntity();
        }

        public static bool TryGetSingletonEntity<T>(this EntityManager em, out Entity entity)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            if (query.CalculateChunkCount() != 1)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            return true;
        }

        public static bool HasSingleton<T>(this EntityManager em)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            return query.CalculateChunkCount() == 1;
        }

        public static T GetSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static void SetSingleton<T>(this EntityManager em, T value, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            query.SetSingleton(value);
        }

        public static RefRW<T> GetSingletonRW<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonRW<T>();
        }

        public static T GetSingletonObject<T>(this EntityManager em, bool completeDependency = true)
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return em.GetComponentObject<T>(query.GetSingletonEntity());
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

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

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em, bool isReadOnly = false, bool completeDependency = true)
            where T : unmanaged, IBufferElementData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonBuffer<T>(isReadOnly);
        }

        public static bool TryGetSingletonBuffer<T>(this EntityManager em, out DynamicBuffer<T> buffer, bool isReadOnly = false, bool completeDependency = true)
            where T : unmanaged, IBufferElementData
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

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
        public static T GetManagedSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : class
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static bool TryGetManagedSingleton<T>(this EntityManager em, out T? component, bool completeDependency = true)
            where T : class
        {
            var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);

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
    }
}
