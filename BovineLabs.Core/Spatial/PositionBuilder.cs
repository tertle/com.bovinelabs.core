// <copyright file="PositionBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Spatial
{
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
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
        private EntityQuery query;
#if ENABLE_TRANSFORM_V1
        private ComponentTypeHandle<Translation> translationHandle;
        private ComponentTypeHandle<LocalToWorld> localToWorldHandle;
        private ComponentTypeHandle<Parent> parentHandle;
#else
        private ComponentTypeHandle<LocalToWorldTransform > localToWorldTransformHandle;
#endif

        /// <summary> Initializes a new instance of the <see cref="PositionBuilder"/> struct. </summary>
        /// <param name="state"> The owning state. </param>
        /// <param name="query"> Query of entities to use. <see cref="Translation"/> will be used if found, otherwise <see cref="LocalToWorld"/>. </param>
        public PositionBuilder(ref SystemState state, EntityQuery query)
        {
            this.query = query;

#if ENABLE_TRANSFORM_V1
            this.translationHandle = state.GetComponentTypeHandle<Translation>(true);
            this.localToWorldHandle = state.GetComponentTypeHandle<LocalToWorld>(true);
            this.parentHandle = state.GetComponentTypeHandle<Parent>(true);
#else
            this.localToWorldTransformHandle = state.GetComponentTypeHandle<LocalToWorldTransform>(true);
#endif

        }

        public JobHandle Gather(ref SystemState state, JobHandle dependency, out NativeArray<SpatialPosition> positions)
        {
#if ENABLE_TRANSFORM_V1
            this.translationHandle.Update(ref state);
            this.localToWorldHandle.Update(ref state);
            this.parentHandle.Update(ref state);
#else
            this.localToWorldTransformHandle.Update(ref state);
#endif

            positions = state.WorldRewindableAllocator.AllocateNativeArray<SpatialPosition>(this.query.CalculateEntityCount());

            var firstEntityIndices = this.query.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependency, out var dependency1);
            dependency = JobHandle.CombineDependencies(dependency, dependency1);

            dependency = new GatherPositionsJob
                {
#if ENABLE_TRANSFORM_V1
                    TranslationHandle = this.translationHandle,
                    LocalToWorldHandle = this.localToWorldHandle,
                    ParentHandle = this.parentHandle,
#else
                    LocalToWorldTransformHandle = this.localToWorldTransformHandle,
#endif
                    Positions = positions,
                    FirstEntityIndices = firstEntityIndices,
                }
                .ScheduleParallel(this.query, dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct GatherPositionsJob : IJobChunk
        {
            [ReadOnly]
#if ENABLE_TRANSFORM_V1
            public ComponentTypeHandle<Translation> TranslationHandle;

            [ReadOnly]
            public ComponentTypeHandle<Parent> ParentHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;
#else
            public ComponentTypeHandle<LocalToWorldTransform> LocalToWorldTransformHandle;
#endif

            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPosition> Positions;

            [ReadOnly]
            public NativeArray<int> FirstEntityIndices;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var ptr = (float3*)this.Positions.GetUnsafePtr();
                var dst = ptr + this.FirstEntityIndices[unfilteredChunkIndex];

                var size = UnsafeUtility.SizeOf<float3>();

                // Is static or has parent, can't use Transform as it's in local space
#if ENABLE_TRANSFORM_V1
                if (!chunk.Has(this.TranslationHandle) || chunk.Has(this.ParentHandle))
                {
                    var ltw = chunk.GetNativeArray(this.LocalToWorldHandle).Reinterpret<float4x4>();
                    var stride = ltw.Slice().SliceWithStride<float3>(UnsafeUtility.SizeOf<float4>() * 3);
                    UnsafeUtility.MemCpyStride(dst, size, stride.GetUnsafeReadOnlyPtr(), stride.Stride, size, ltw.Length);
                }
                else
#endif
                {
#if ENABLE_TRANSFORM_V1
                    var translations = chunk.GetNativeArray(this.TranslationHandle);
                    var src = translations.GetUnsafeReadOnlyPtr();
                    UnsafeUtility.MemCpy(dst, src, size * translations.Length);
#else
                    var localToWorldTransforms = chunk.GetNativeArray(this.LocalToWorldTransformHandle).Reinterpret<UniformScaleTransform>();
                    var stride = localToWorldTransforms.Slice().SliceWithStride<float3>(0);
                    UnsafeUtility.MemCpyStride(dst, size, stride.GetUnsafeReadOnlyPtr(), stride.Stride, size, localToWorldTransforms.Length);
#endif
                }
            }
        }
    }
}
