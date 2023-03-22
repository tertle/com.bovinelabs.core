// <copyright file="CopyTransformToGameObjectSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_COPY_TRANSFORM
namespace BovineLabs.Core.Hybrid
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Jobs;

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class CopyTransformToGameObjectSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            this.WorldTransform();
            this.LocalTransform();
        }

        private void WorldTransform()
        {
            var worldTransformQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Transform>().WithAll<CopyTransformToGameObject, WorldTransform>()
                .Build();

            if (worldTransformQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var worldTransformAccess = worldTransformQuery.GetTransformAccessArray();
            var worldTransforms = worldTransformQuery.ToComponentDataListAsync<WorldTransform>(this.WorldUpdateAllocator, out var dependency);
            this.Dependency = JobHandle.CombineDependencies(this.Dependency, dependency);
            this.Dependency = new CopyWorldTransformsJob { WorldTransforms = worldTransforms.AsDeferredJobArray() }
                .Schedule(worldTransformAccess, this.Dependency);
        }

        private void LocalTransform()
        {
            var localTransformQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Transform>().WithAll<CopyTransformToGameObject, LocalTransform>().WithNone<WorldTransform>()
                .Build();

            if (localTransformQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            var localTransformAccess = localTransformQuery.GetTransformAccessArray();
            var localTransforms = localTransformQuery.ToComponentDataListAsync<LocalTransform>(this.WorldUpdateAllocator, out var dependency);
            this.Dependency = JobHandle.CombineDependencies(this.Dependency, dependency);
            this.Dependency = new CopyLocalTransformsJob { LocalTransforms = localTransforms.AsDeferredJobArray() }
                .Schedule(localTransformAccess, this.Dependency);
        }

        [BurstCompile]
        private struct CopyWorldTransformsJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<WorldTransform> WorldTransforms;

            public void Execute(int index, TransformAccess transform)
            {
                var value = this.WorldTransforms[index];
                transform.position = value.Position;
                transform.rotation = value.Rotation;
            }
        }

        [BurstCompile]
        private struct CopyLocalTransformsJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeArray<LocalTransform> LocalTransforms;

            public void Execute(int index, TransformAccess transform)
            {
                var value = this.LocalTransforms[index];
                transform.position = value.Position;
                transform.rotation = value.Rotation;
            }
        }
    }
}
#endif
