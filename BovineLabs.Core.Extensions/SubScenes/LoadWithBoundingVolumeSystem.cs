// <copyright file="LoadWithBoundingVolumeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using Unity.Transforms;

    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct LoadWithBoundingVolumeSystem : ISystem
    {
        private EntityQuery loadingQuery;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.loadingQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, LoadsSubScene>().Build();
            state.RequireForUpdate(this.loadingQuery);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var loaderPositions = this.loadingQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);

            if (!SystemAPI.TryGetSingleton<LoadWithBoundingVolumeConfig>(out var config))
            {
                // If we haven't configured this, just always load everything
                config.LoadMaxDistance = 45000;
                config.UnloadMaxDistance = 45000;
            }

            var loadMaxDistanceSq = config.LoadMaxDistance * config.LoadMaxDistance;
            var unloadMaxDistanceSq = config.UnloadMaxDistance * config.UnloadMaxDistance;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (loadRO, resolvedSectionEntity, entity) in SystemAPI.Query<RefRO<LoadWithBoundingVolume>, DynamicBuffer<ResolvedSectionEntity>>()
                         .WithAll<ResolvedSectionEntity>().WithEntityAccess())
            {
                ref readonly var load = ref loadRO.ValueRO;
                var loadDistanceSq = load.LoadMaxDistanceOverrideSq > 0 ? load.LoadMaxDistanceOverrideSq : loadMaxDistanceSq;
                var unloadDistanceSq = load.UnloadMaxDistanceOverrideSq > 0 ? load.UnloadMaxDistanceOverrideSq : unloadMaxDistanceSq;

                var anyInRange = false;
                var allOutOfRange = true;

                foreach (var position in loaderPositions)
                {
                    var distanceSq = load.Bounds.DistanceSq(position.Position);
                    anyInRange = distanceSq < loadDistanceSq;
                    allOutOfRange &= distanceSq > unloadDistanceSq;
                }

                if (anyInRange)
                {
                    if (!SystemAPI.HasComponent<RequestSceneLoaded>(entity))
                    {
                        LoadScene(ecb, entity, resolvedSectionEntity);
                    }
                }
                else if (allOutOfRange)
                {
                    if (SystemAPI.HasComponent<RequestSceneLoaded>(entity))
                    {
                        UnloadScene(ecb, entity, resolvedSectionEntity);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }

        private static void UnloadScene(EntityCommandBuffer entityCommandBuffer, Entity entity, DynamicBuffer<ResolvedSectionEntity> resolvedSectionEntity)
        {
            entityCommandBuffer.RemoveComponent<RequestSceneLoaded>(entity);
            foreach (var section in resolvedSectionEntity.AsNativeArray())
            {
                entityCommandBuffer.RemoveComponent<RequestSceneLoaded>(section.SectionEntity);
            }
        }

        private static void LoadScene(EntityCommandBuffer entityCommandBuffer, Entity entity, DynamicBuffer<ResolvedSectionEntity> resolvedSectionEntity)
        {
            entityCommandBuffer.AddComponent<RequestSceneLoaded>(entity);
            foreach (var section in resolvedSectionEntity.AsNativeArray())
            {
                entityCommandBuffer.AddComponent<RequestSceneLoaded>(section.SectionEntity);
            }
        }
    }
}
#endif
