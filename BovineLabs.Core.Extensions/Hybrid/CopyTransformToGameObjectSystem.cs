// <copyright file="CopyTransformToGameObjectSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_COPY_TRANSFORM
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
            var localTransformQuery = SystemAPI.QueryBuilder()
                .WithAllRW<Transform>().WithAll<CopyTransformToGameObject, LocalTransform>()
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
