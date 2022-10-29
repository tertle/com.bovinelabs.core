// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary> Extensions for <see cref="EntityManager"/>. </summary>
    public static class EntityManagerExtensions
    {

        public static unsafe SharedComponentDataFromIndex<T> GetSharedComponentDataFromEntity<T>(this EntityManager entityManager, bool isReadOnly = true)
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

        /// <summary> Gets or creates the <see cref="T"/> singleton entity. </summary>
        /// <param name="em"> The entity manager. </param>
        /// <typeparam name="T"> The singleton type. </typeparam>
        /// <returns> The entity. </returns>
        public static Entity GetOrCreateSingletonEntity<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.IsEmptyIgnoreFilter ? em.CreateEntity(typeof(T)) : query.GetSingletonEntity();
        }

        public static Entity GetSingletonEntity<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingletonEntity();
        }

        public static void SetSingletonEntity<T>(this EntityManager em, T value)
            where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<T>());
            query.SetSingleton(value);
        }

        public static bool TryGetSingletonEntity<T>(this EntityManager em, out Entity entity)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());

            if (query.CalculateChunkCount() == 0)
            {
                entity = Entity.Null;
                return false;
            }

            entity = query.GetSingletonEntity();
            return true;
        }

        public static bool HasSingleton<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateChunkCount() != 0;
        }

        public static T GetSingleton<T>(this EntityManager em)
            where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingleton<T>();
        }

        public static T GetSingletonObject<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return em.GetComponentObject<T>(query.GetSingletonEntity());
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component)
            where T : unmanaged, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());

            if (query.CalculateEntityCount() == 0)
            {
                component = default;
                return false;
            }

            component = query.GetSingleton<T>();
            return true;
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return em.GetBuffer<T>(query.GetSingletonEntity(), isReadOnly);
        }

        public static bool TryGetSingletonBuffer<T>(this EntityManager em, out DynamicBuffer<T> buffer, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());

            if (query.CalculateEntityCount() == 0)
            {
                buffer = default;
                return false;
            }

            var entity = query.GetSingletonEntity();
            buffer = em.GetBuffer<T>(entity, isReadOnly);
            return true;
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public static T GetManagedSingleton<T>(this EntityManager em)
            where T : class, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<T>());
            return query.GetSingleton<T>();
        }

        public static bool TryGetManagedSingleton<T>(this EntityManager em, out T component)
            where T : class, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadWrite<T>());

            if (query.CalculateEntityCount() == 0)
            {
                component = default;
                return false;
            }

            component = query.GetSingleton<T>();
            return true;
        }
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertHasComponent<T>(this EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<T>(entity))
            {
                throw new ArgumentException($"Entity {entity} has no {nameof(T)}");
            }
        }
    }
}
