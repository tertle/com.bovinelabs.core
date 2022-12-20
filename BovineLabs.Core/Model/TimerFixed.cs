﻿// <copyright file="TimerFixed.cs" company="BovineLabs">
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

    public struct TimerFixed<TOn, TRemaining, TActive>
        where TOn : unmanaged, IComponentData
        where TRemaining : unmanaged, IComponentData
        where TActive : unmanaged, IComponentData
    {
        private readonly float duration;
        private ComponentTypeHandle<TOn> onHandle;
        private ComponentTypeHandle<TRemaining> remainingHandle;
        private ComponentTypeHandle<TActive> activeHandle;
        private EntityQuery query;

        public TimerFixed(float duration)
        {
            this = default;
            this.duration = duration;
        }

        public void OnCreate(ref SystemState state)
        {
            Assert.AreNotEqual(0, this.duration, "No duration set for TimerFixed. Use the constructor.");

            Assert.AreEqual(UnsafeUtility.SizeOf<bool>(), UnsafeUtility.SizeOf<TOn>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TRemaining>());
            Assert.AreEqual(UnsafeUtility.SizeOf<bool>(), UnsafeUtility.SizeOf<TActive>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TRemaining>());

            this.onHandle = state.GetComponentTypeHandle<TOn>();
            this.remainingHandle = state.GetComponentTypeHandle<TRemaining>();
            this.activeHandle = state.GetComponentTypeHandle<TActive>(true);

            this.query = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<TRemaining>(),
                    ComponentType.ReadWrite<TOn>(),
                    ComponentType.ReadOnly<TActive>(),
                },
            });
        }

        public void OnUpdate(ref SystemState state, UpdateTimeJob job)
        {
            this.onHandle.Update(ref state);
            this.remainingHandle.Update(ref state);
            this.activeHandle.Update(ref state);

            job.OnHandle = this.onHandle;
            job.RemainingHandle = this.remainingHandle;
            job.ActiveHandle = this.activeHandle;
            job.Duration = this.duration;
            job.DeltaTime = state.WorldUnmanaged.Time.DeltaTime;
            job.SystemVersion = state.LastSystemVersion;

            state.Dependency = job.ScheduleParallel(this.query, state.Dependency);
        }

        [BurstCompile]
        public unsafe struct UpdateTimeJob : IJobChunk
        {
            public ComponentTypeHandle<TOn> OnHandle;
            public ComponentTypeHandle<TRemaining> RemainingHandle;

            [ReadOnly]
            public ComponentTypeHandle<TActive> ActiveHandle;

            public float Duration;

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
                    var remainings = chunk.GetNativeArray(ref this.RemainingHandle).Reinterpret<float>();
                    var triggers = chunk.GetNativeArray(ref this.ActiveHandle).Reinterpret<bool>();
                    var durationOns = (bool*)chunk.GetComponentDataPtrRO(ref this.OnHandle);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (triggers[i] && !durationOns[i])
                        {
                            remainings[i] = this.Duration;
                        }
                    }
                }

                if (activeChanged || chunk.DidChange(ref this.RemainingHandle, this.SystemVersion))
                {
                    var remainings = chunk.GetNativeArray(ref this.RemainingHandle).Reinterpret<float>();

                    if (!this.onBuffer.IsCreated)
                    {
                        this.onBuffer = new NativeList<bool>(remainings.Length, Allocator.Temp);
                    }

                    this.onBuffer.ResizeUninitialized(remainings.Length);

                    for (var i = 0; i < remainings.Length; i++)
                    {
                        remainings[i] = math.max(0, remainings[i] - this.DeltaTime);
                        this.onBuffer[i] = remainings[i] != 0;
                    }

                    var original = chunk.GetComponentDataPtrRO(ref this.OnHandle);
                    var updated = this.onBuffer.GetUnsafeReadOnlyPtr();
                    var hasChanged = UnsafeUtility.MemCmp(original, updated, UnsafeUtility.SizeOf<bool>() * this.onBuffer.Length) != 0;

                    if (hasChanged)
                    {
                        var ons = chunk.GetNativeArray(ref this.OnHandle);
                        ons.Reinterpret<bool>().CopyFrom(this.onBuffer.AsArray());
                    }
                }
            }
        }
    }
}
