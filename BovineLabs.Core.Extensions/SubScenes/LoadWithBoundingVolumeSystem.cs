// <copyright file="LoadWithBoundingVolumeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Transforms;

    [UpdateInGroup(typeof(AfterTransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct LoadWithBoundingVolumeSystem : ISystem
    {
        private EntityQuery loadingQuery;
        private SubSceneUtil subSceneUtil;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.loadingQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, LoadsSubScene>().Build();
            state.RequireForUpdate(this.loadingQuery);

            this.subSceneUtil = new SubSceneUtil(ref state);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            this.subSceneUtil.Update(ref state);
            var loaderPositions = this.loadingQuery.ToComponentDataArray<LocalTransform>(state.WorldUpdateAllocator);
            var ecb = SystemAPI.GetSingleton<InstantiateCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (loadRO, entity) in SystemAPI.Query<RefRO<LoadWithBoundingVolume>>().WithEntityAccess())
            {
                ref readonly var load = ref loadRO.ValueRO;

                var anyInRange = false;
                var allOutOfRange = true;

                foreach (var position in loaderPositions)
                {
                    var distanceSq = load.Bounds.DistanceSq(position.Position);
                    anyInRange = distanceSq < load.LoadMaxDistanceSq;
                    allOutOfRange &= distanceSq > load.UnloadMaxDistanceSq;
                }

                if (anyInRange)
                {
                    if (!SystemAPI.HasComponent<RequestSceneLoaded>(entity))
                    {
                        this.subSceneUtil.LoadScene(ecb, entity);
                    }
                }
                else if (allOutOfRange)
                {
                    if (SystemAPI.HasComponent<RequestSceneLoaded>(entity))
                    {
                        this.subSceneUtil.UnloadScene(ecb, entity);
                    }
                }
            }
        }
    }
}
#endif
