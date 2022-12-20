// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Extensions for <see cref="EntityManager" />. </summary>
    public static class EntityManagerExtensions
    {
        public static unsafe SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this EntityManager entityManager, bool isReadOnly = true)
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

        public static unsafe SharedComponentLookup<T> GetSharedComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = true)
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

        public static unsafe UnsafeEnableableLookup GetUnsafeEnableableLookup(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            return new UnsafeEnableableLookup(access);
        }

        /// <summary> Gets or creates the <see cref="T" /> singleton entity. </summary>
        /// <param name="em"> The entity manager. </param>
        /// <typeparam name="T"> The singleton type. </typeparam>
        /// <returns> The entity. </returns>
        public static Entity GetOrCreateSingletonEntity<T>(this EntityManager em)
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
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
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            return query.GetSingletonEntity();
        }

        public static void SetSingleton<T>(this EntityManager em, T value)
            where T : unmanaged, IComponentData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            query.SetSingleton(value);
        }

        public static bool TryGetSingletonEntity<T>(this EntityManager em, out Entity entity)
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);

            if (query.CalculateChunkCount() != 1)
            {
                entity = Entity.Null;
                return false;
            }

            query.CompleteDependency();
            entity = query.GetSingletonEntity();
            return true;
        }

        public static bool HasSingleton<T>(this EntityManager em)
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            return query.CalculateChunkCount() == 1;
        }

        public static T GetSingleton<T>(this EntityManager em)
            where T : unmanaged, IComponentData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            return query.GetSingleton<T>();
        }

        public static T GetSingletonObject<T>(this EntityManager em)
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            return em.GetComponentObject<T>(query.GetSingletonEntity());
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component)
            where T : unmanaged, IComponentData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);

            if (query.CalculateEntityCount() != 1)
            {
                component = default;
                return false;
            }

            query.CompleteDependency();
            component = query.GetSingleton<T>();
            return true;
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            return query.GetSingletonBuffer<T>(isReadOnly);
        }

        public static bool TryGetSingletonBuffer<T>(this EntityManager em, out DynamicBuffer<T> buffer, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);

            if (query.CalculateEntityCount() == 0)
            {
                buffer = default;
                return false;
            }

            query.CompleteDependency();
            buffer = query.GetSingletonBuffer<T>(isReadOnly);
            return true;
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public static T GetManagedSingleton<T>(this EntityManager em)
            where T : class, IComponentData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);
            query.CompleteDependency();
            return query.GetSingleton<T>();
        }

        public static bool TryGetManagedSingleton<T>(this EntityManager em, out T component)
            where T : class, IComponentData
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(EntityQueryOptions.IncludeSystems);
            var query = em.CreateEntityQuery(builder);

            if (query.CalculateEntityCount() != 1)
            {
                component = default;
                return false;
            }

            query.CompleteDependency();
            component = query.GetSingleton<T>();
            return true;
        }
#endif
    }
}
