// <copyright file="PositionBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.Transforms;

    public struct SpatialPosition : ISpatialPosition
    {
        public float3 Position;

        float2 ISpatialPosition.Position => this.Position.xz;
    }

    public struct PositionBuilder
    {
        private readonly SystemBase system;
        private EntityQuery query;
        private ComponentTypeHandle<Translation> translationHandle;
        private ComponentTypeHandle<LocalToWorld> localToWorldHandle;
        private ComponentTypeHandle<Parent> parentHandle;

        /// <summary> Initializes a new instance of the <see cref="PositionBuilder"/> struct. </summary>
        /// <param name="system"> The owning system. </param>
        /// <param name="query"> Query of entities to use. <see cref="Translation"/> will be used if found, otherwise <see cref="LocalToWorld"/>. </param>
        public PositionBuilder(SystemBase system, EntityQuery query)
        {
            this.system = system;
            this.query = query;

            this.translationHandle = system.GetComponentTypeHandle<Translation>(true);
            this.localToWorldHandle = system.GetComponentTypeHandle<LocalToWorld>(true);
            this.parentHandle = system.GetComponentTypeHandle<Parent>(true);
        }

        public JobHandle Gather(JobHandle dependency, out NativeArray<SpatialPosition> positions)
        {
            this.translationHandle.Update(this.system);
            this.localToWorldHandle.Update(this.system);
            this.parentHandle.Update(this.system);

            positions = this.system.World.UpdateAllocator.AllocateNativeArray<SpatialPosition>(this.query.CalculateEntityCount());

            dependency = new GatherPositionsJob
                {
                    TranslationHandle = this.translationHandle,
                    LocalToWorldHandle = this.localToWorldHandle,
                    ParentHandle = this.parentHandle,
                    Positions = positions,
                }
                .ScheduleParallel(this.query, dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct GatherPositionsJob : IJobEntityBatchWithIndex
        {
            [ReadOnly]
            public ComponentTypeHandle<Translation> TranslationHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

            [ReadOnly]
            public ComponentTypeHandle<Parent> ParentHandle;

            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPosition> Positions;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex, int indexOfFirstEntityInQuery)
            {
                var ptr = (float3*)this.Positions.GetUnsafePtr();
                var dst = ptr + indexOfFirstEntityInQuery;

                var size = UnsafeUtility.SizeOf<float3>();

                // Is static or has parent, can't use Transform as it's in local space
                if (!batchInChunk.Has(this.TranslationHandle) || batchInChunk.Has(this.ParentHandle))
                {
                    var ltw = batchInChunk.GetNativeArray(this.LocalToWorldHandle).Reinterpret<float4x4>();
                    var stride = ltw.Slice().SliceWithStride<float3>(UnsafeUtility.SizeOf<float4>() * 3);
                    UnsafeUtility.MemCpyStride(dst, size, stride.GetUnsafeReadOnlyPtr(), stride.Stride, size, ltw.Length);
                }
                else
                {
                    var translations = batchInChunk.GetNativeArray(this.TranslationHandle);
                    var src = translations.GetUnsafeReadOnlyPtr();
                    UnsafeUtility.MemCpy(dst, src, size * translations.Length);
                }
            }
        }
    }
}
