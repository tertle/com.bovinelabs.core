// <copyright file="Timer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Model
{
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
#if UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS
    using Unity.Burst.CompilerServices;
#endif

    public struct Timer<TOn, TRemaining, TActive, TDuration>
        where TOn : unmanaged, IComponentData
        where TRemaining : unmanaged, IComponentData
        where TActive : unmanaged, IComponentData
        where TDuration : unmanaged, IComponentData
    {
        private ComponentTypeHandle<TOn> onHandle;
        private ComponentTypeHandle<TRemaining> remainingHandle;
        private ComponentTypeHandle<TActive> activeHandle;
        private ComponentTypeHandle<TDuration> durationHandle;
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            Assert.AreEqual(UnsafeUtility.SizeOf<bool>(), UnsafeUtility.SizeOf<TOn>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TRemaining>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TDuration>());

            this.onHandle = state.GetComponentTypeHandle<TOn>();
            this.remainingHandle = state.GetComponentTypeHandle<TRemaining>();
            this.activeHandle = state.GetComponentTypeHandle<TActive>(true);
            this.durationHandle = state.GetComponentTypeHandle<TDuration>(true);

            this.query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TRemaining, TOn>()
                .WithAll<TActive, TDuration>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state, UpdateTimeJob job = default)
        {
            this.onHandle.Update(ref state);
            this.remainingHandle.Update(ref state);
            this.activeHandle.Update(ref state);
            this.durationHandle.Update(ref state);

            job.OnHandle = this.onHandle;
            job.RemainingHandle = this.remainingHandle;
            job.ActiveHandle = this.activeHandle;
            job.DurationHandle = this.durationHandle;
            job.DeltaTime = state.WorldUnmanaged.Time.DeltaTime;
            job.SystemVersion = state.LastSystemVersion;

            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }

        [NoAlias]
        [BurstCompile]
        public unsafe struct UpdateTimeJob : IJobChunk
        {
            public ComponentTypeHandle<TOn> OnHandle;
            public ComponentTypeHandle<TRemaining> RemainingHandle;

            [ReadOnly]
            public ComponentTypeHandle<TActive> ActiveHandle;

            [ReadOnly]
            public ComponentTypeHandle<TDuration> DurationHandle;

            public float DeltaTime;
            public uint SystemVersion;

            [NativeDisableContainerSafetyRestriction] // Only initialized in the job
            private NativeList<bool> onBuffer;

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var activeChanged = chunk.DidChange(ref this.ActiveHandle, this.SystemVersion);

                if (activeChanged)
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref this.RemainingHandle);
                    var triggers = (bool*)chunk.GetRequiredComponentDataPtrRO(ref this.ActiveHandle);
                    var durations = (float*)chunk.GetRequiredComponentDataPtrRO(ref this.DurationHandle);
                    var durationOns = (bool*)chunk.GetRequiredComponentDataPtrRO(ref this.OnHandle);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (triggers[i] && !durationOns[i])
                        {
                            remainings[i] = durations[i];
                        }
                    }
                }

                if (activeChanged || chunk.DidChange(ref this.RemainingHandle, this.SystemVersion))
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref this.RemainingHandle);

                    if (!this.onBuffer.IsCreated)
                    {
                        this.onBuffer = new NativeList<bool>(chunk.Count, Allocator.Temp);
                    }

                    this.onBuffer.ResizeUninitialized(chunk.Count);
                    CalculateOnVectorized(remainings, this.onBuffer.GetUnsafeReadOnlyPtr(), this.onBuffer.Length, this.DeltaTime);

                    // We open RO to avoid change filter trigger unless it has changed
                    var original = chunk.GetRequiredComponentDataPtrRO(ref this.OnHandle);
                    var updated = this.onBuffer.GetUnsafeReadOnlyPtr();
                    var hasChanged = UnsafeUtility.MemCmp(original, updated, UnsafeUtility.SizeOf<bool>() * this.onBuffer.Length) != 0;

                    if (hasChanged)
                    {
                        var ons = chunk.GetNativeArray(ref this.OnHandle).Reinterpret<bool>();
                        ons.CopyFrom(this.onBuffer.AsArray());
                    }
                }
            }

            private static void CalculateOnVectorized([NoAlias] float* remainings, [NoAlias] bool* isOn, int length, float deltaTime)
            {
                for (var i = 0; i < length; i++)
                {
#if UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS
                    Loop.ExpectVectorized();
#endif
                    remainings[i] = math.max(0, remainings[i] - deltaTime);
                    isOn[i] = remainings[i] != 0;
                }
            }
        }
    }
}
