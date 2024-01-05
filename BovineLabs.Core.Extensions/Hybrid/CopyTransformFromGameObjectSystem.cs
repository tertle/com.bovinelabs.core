// <copyright file="CopyTransformFromGameObjectSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Hybrid
{
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEngine;
    using UnityEngine.Jobs;

    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(BeginSimulationSystemGroup))]
    public partial class CopyTransformFromGameObjectSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var transformQuery = SystemAPI.QueryBuilder().WithAllRW<LocalTransform>().WithAll<CopyTransformFromGameObject, Transform>().Build();

            var transforms = transformQuery.GetTransformAccessArray();
            var transformStashes = this.World.UpdateAllocator.AllocateNativeArray<TransformStash>(transforms.length);

            this.Dependency = new StashTransforms { TransformStashes = transformStashes }.Schedule(transforms, this.Dependency);
            new CopyTransforms { TransformStashes = transformStashes }.ScheduleParallel(transformQuery);
        }

        private struct TransformStash
        {
            public float3 Position;
            public quaternion Rotation;
        }

        [BurstCompile]
        private struct StashTransforms : IJobParallelForTransform
        {
            public NativeArray<TransformStash> TransformStashes;

            public void Execute(int index, TransformAccess transform)
            {
                this.TransformStashes[index] = new TransformStash
                {
                    Rotation = transform.rotation,
                    Position = transform.position,
                };
            }
        }

        [BurstCompile]
        [WithAll(typeof(CopyTransformFromGameObject), typeof(Transform))]
        private partial struct CopyTransforms : IJobEntity
        {
            [ReadOnly]
            public NativeArray<TransformStash> TransformStashes;

            private void Execute([EntityIndexInQuery] int entityInQueryIndex, ref LocalTransform localTransform)
            {
                var transformStash = this.TransformStashes[entityInQueryIndex];
                localTransform.Position = transformStash.Position;
                localTransform.Rotation = transformStash.Rotation;
            }
        }
    }
}
#endif
