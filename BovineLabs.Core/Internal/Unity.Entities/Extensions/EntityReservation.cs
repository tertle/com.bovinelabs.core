namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    /// <summary>
    /// Reserves entity ids before their entities exist, as <see cref="EntityCommandBuffer.Instantiate(Entity)"/> does at record time, so a
    /// deferred command can hand back the entity it will create. Thread safe. A reserved id is filled by
    /// <see cref="EntityManagerExtensions.InstantiateExisting{T}"/> or returned with <see cref="Release(Entity*, int)"/>; until then it does not
    /// exist in any world.
    /// </summary>
    public static unsafe class EntityReservation
    {
        /// <summary>Reserves one entity id.</summary>
        public static Entity Reserve()
        {
            Entity entity;
            EntityComponentStore.s_entityStore.Data.AllocateEntities(&entity, 1);
            return entity;
        }

        /// <summary>Reserves <paramref name="count"/> entity ids into <paramref name="entities"/>.</summary>
        public static void Reserve(Entity* entities, int count)
        {
            EntityComponentStore.s_entityStore.Data.AllocateEntities(entities, count);
        }

        /// <summary>Reserves one entity id per element of <paramref name="entities"/>.</summary>
        public static void Reserve(NativeArray<Entity> entities)
        {
            Reserve((Entity*)entities.GetUnsafePtr(), entities.Length);
        }

        /// <summary>Returns reserved ids that were never filled.</summary>
        public static void Release(Entity* entities, int count)
        {
            EntityComponentStore.s_entityStore.Data.DeallocateEntities(entities, count);
        }
    }
}
