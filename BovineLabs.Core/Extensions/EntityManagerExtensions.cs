// <copyright file="EntityManagerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    /// <summary> Physics extensions. </summary>
    public static class EntityManagerExtensions
    {
        /// <summary> Gets or creates the <see cref="T"/> singleton entity. </summary>
        /// <remarks>
        /// This is currently required for working around ISystemBase limitations since GetSingleton, Query.GetX don't seem to work yet.
        /// Will be obsoleted when this support is added to ISystemBase.
        /// </remarks>
        /// <param name="em"> The entity manager. </param>
        /// <typeparam name="T"> The singleton type. </typeparam>
        /// <returns> The entity. </returns>
        public static Entity GetOrCreateSingletonEntity<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateChunkCount() == 0 ? em.CreateEntity(typeof(T)) : query.GetSingletonEntity();
        }

        public static Entity GetSingletonEntity<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingletonEntity();
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

        public static bool HasSingletonEntity<T>(this EntityManager em)
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.CalculateChunkCount() != 0;
        }

        public static T GetSingleton<T>(this EntityManager em)
            where T : struct, IComponentData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return query.GetSingleton<T>();
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component)
            where T : struct, IComponentData
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

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em)
            where T : struct, IBufferElementData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());
            return em.GetBuffer<T>(query.GetSingletonEntity());
        }

        public static bool TryGetSingletonBuffer<T>(this EntityManager em, out DynamicBuffer<T> buffer)
            where T : struct, IBufferElementData
        {
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<T>());

            if (query.CalculateEntityCount() == 0)
            {
                buffer = default;
                return false;
            }

            var entity = query.GetSingletonEntity();
            buffer = em.GetBuffer<T>(entity);
            return true;
        }
    }
}