// <copyright file="TimerEnableable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Model
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct TimerEnableable<TOn, TRemaining, TActive, TDuration>
        where TOn : unmanaged, IComponentData, IEnableableComponent
        where TRemaining : unmanaged, IComponentData
        where TActive : unmanaged, IComponentData, IEnableableComponent
        where TDuration : unmanaged, IComponentData
    {
        private ComponentTypeHandle<TOn> onHandle;
        private ComponentTypeHandle<TRemaining> remainingHandle;
        private ComponentTypeHandle<TActive> activeHandle;
        private ComponentTypeHandle<TDuration> durationHandle;
        private EntityQuery query;

        public void OnCreate(ref SystemState state)
        {
            Check.Assume(UnsafeUtility.SizeOf<float>() == UnsafeUtility.SizeOf<TRemaining>());
            Check.Assume(UnsafeUtility.SizeOf<float>() == UnsafeUtility.SizeOf<TDuration>());

            this.onHandle = state.GetComponentTypeHandle<TOn>();
            this.remainingHandle = state.GetComponentTypeHandle<TRemaining>();
            this.activeHandle = state.GetComponentTypeHandle<TActive>(true);
            this.durationHandle = state.GetComponentTypeHandle<TDuration>(true);

            this.query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TRemaining, TOn>()
                .WithAll<TActive, TDuration>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
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

            /// <inheritdoc />
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var activeChanged = chunk.DidChange(ref this.ActiveHandle, this.SystemVersion);

                if (activeChanged)
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref this.RemainingHandle);
                    var durations = (float*)chunk.GetRequiredComponentDataPtrRO(ref this.DurationHandle);
                    var durationOnBits = chunk.GetRequiredEnabledBitsRO(ref this.OnHandle);
                    var triggerBits = chunk.GetRequiredEnabledBitsRO(ref this.ActiveHandle);
                    var durationOns = (ulong*)&durationOnBits;
                    var triggers = (ulong*)&triggerBits;

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (Bitwise.IsSet(triggers, i) && !Bitwise.IsSet(durationOns, i))
                        {
                            remainings[i] = durations[i];
                        }
                    }
                }

                if (activeChanged || chunk.DidChange(ref this.RemainingHandle, this.SystemVersion))
                {
                    // We open RO to avoid change filter trigger unless it has changed
                    ref readonly var original = ref chunk.GetRequiredEnabledBitsRO(ref this.OnHandle);
                    var updated = original;
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref this.RemainingHandle);

                    CalculateOn(remainings, (ulong*)&updated, chunk.Count, this.DeltaTime);

                    var hasChanged = updated.ULong0 != original.ULong0 || updated.ULong1 != original.ULong1;

                    if (hasChanged)
                    {
                        ref var enabledBits = ref chunk.GetRequiredEnabledBitsRW(ref this.OnHandle, out var count);
                        enabledBits = updated;

                        *count = chunk.Count - math.countbits(enabledBits.ULong0) - math.countbits(enabledBits.ULong1);
                    }
                }
            }

            private static void CalculateOn([NoAlias] float* remainings, [NoAlias] ulong* isOn, int length, float deltaTime)
            {
                var u0 = isOn;
                var length0 = math.min(64, length);

                for (var i = 0; i < length0; i++)
                {
                    remainings[i] = math.max(0, remainings[i] - deltaTime);
                    UnsafeBitArray.Set(u0, i, remainings[i] != 0);
                }

                var u1 = isOn + 1;
                var length1 = math.min(64, length - 64);

                for (var i = 0; i < length1; i++)
                {
                    remainings[i + 64] = math.max(0, remainings[i + 64] - deltaTime);
                    UnsafeBitArray.Set(u1, i, remainings[i + 64] != 0);
                }
            }
        }
    }
}
