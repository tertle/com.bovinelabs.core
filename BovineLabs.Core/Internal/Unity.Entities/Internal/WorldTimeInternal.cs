namespace BovineLabs.Core.Internal
{
    using Unity.Core;
    using Unity.Entities;

    public static class WorldTimeInternal
    {
        public static ComponentType TimeComponentType => ComponentType.ReadWrite<WorldTime>();

        public static ComponentType QueueComponentType => ComponentType.ReadWrite<WorldTimeQueue>();

        public static void SetTime(EntityManager entityManager, Entity entity, TimeData time)
        {
            entityManager.SetComponentData(entity, new WorldTime { Time = time });
        }
    }
}
