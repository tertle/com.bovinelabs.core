// <copyright file="DestroyTimer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Utility for managing timer-based entity destruction. Automatically enables DestroyEntity when the timer component reaches zero.
    /// The timer component T must be the same size as a float.
    /// </summary>
    /// <typeparam name="T">The timer component type that will be decremented each frame.</typeparam>
    public struct DestroyTimer<T>
        where T : unmanaged, IComponentData
    {
        private ComponentTypeHandle<DestroyEntity> entityDestroyHandle;
        private ComponentTypeHandle<T> remainingHandle;
        private EntityQuery query;

        /// <summary>
        /// Initializes the DestroyTimer for use. Must be called in the system's OnCreate method.
        /// </summary>
        /// <param name="state">The system state.</param>
        public void OnCreate(ref SystemState state)
        {
            Check.Assume(UnsafeUtility.SizeOf<float>() == UnsafeUtility.SizeOf<T>());

            this.entityDestroyHandle = state.GetComponentTypeHandle<DestroyEntity>();
            this.remainingHandle = state.GetComponentTypeHandle<T>();

            this.query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithDisabledRW<DestroyEntity>().Build(ref state);
        }

        /// <summary>
        /// Updates all timer components and enables DestroyEntity when timers reach zero.
        /// </summary>
        /// <param name="state">The system state.</param>
        /// <param name="job">Optional custom job instance. If not provided, uses default.</param>
        public void OnUpdate(ref SystemState state, UpdateTimeJob job = default)
        {
            this.entityDestroyHandle.Update(ref state);
            this.remainingHandle.Update(ref state);

            job.EntityDestroyHandle = this.entityDestroyHandle;
            job.RemainingHandle = this.remainingHandle;
            job.DeltaTime = state.WorldUnmanaged.Time.DeltaTime;

            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }

        /// <summary>
        /// Job that decrements timer values and enables DestroyEntity when timers reach zero.
        /// </summary>
        [BurstCompile]
        public unsafe struct UpdateTimeJob : IJobChunk
        {
            public ComponentTypeHandle<DestroyEntity> EntityDestroyHandle;
            public ComponentTypeHandle<T> RemainingHandle;
            public float DeltaTime;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref this.RemainingHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndex))
                {
                    remainings[entityIndex] = math.max(0, remainings[entityIndex] - this.DeltaTime);
                    if (remainings[entityIndex] == 0)
                    {
                        chunk.SetComponentEnabled(ref this.EntityDestroyHandle, entityIndex, true);
                    }
                }
            }
        }
    }
}
#endif
