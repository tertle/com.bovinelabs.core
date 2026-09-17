namespace BovineLabs.Core
{
    using Unity.Entities;

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [UpdateInGroup(typeof(TransformBakingSystemGroup), OrderFirst = true)]
    internal partial struct BLDebugBakingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            this.EnsureLogger(ref state);
        }

        public void OnUpdate(ref SystemState state)
        {
            this.EnsureLogger(ref state);
        }

        private void EnsureLogger(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<BLLogger>(out var logger))
            {
                BLDebugSystem.Create(state.World);
                logger = SystemAPI.GetSingletonEntity<BLLogger>();
            }

            if (!state.EntityManager.HasComponent<BakingOnlyEntity>(logger))
            {
                state.EntityManager.AddComponent<BakingOnlyEntity>(logger);
            }
        }
    }
}
