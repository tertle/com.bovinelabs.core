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
        private ComponentTypeHandle<TOn> _onHandle;
        private ComponentTypeHandle<TRemaining> _remainingHandle;
        private ComponentTypeHandle<TActive> _activeHandle;
        private ComponentTypeHandle<TDuration> _durationHandle;
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            Check.Assume(UnsafeUtility.SizeOf<float>() == UnsafeUtility.SizeOf<TRemaining>());
            Check.Assume(UnsafeUtility.SizeOf<float>() == UnsafeUtility.SizeOf<TDuration>());

            _onHandle = state.GetComponentTypeHandle<TOn>();
            _remainingHandle = state.GetComponentTypeHandle<TRemaining>();
            _activeHandle = state.GetComponentTypeHandle<TActive>(true);
            _durationHandle = state.GetComponentTypeHandle<TDuration>(true);

            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TRemaining, TOn>()
                .WithAll<TActive, TDuration>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState | EntityQueryOptions.FilterWriteGroup)
                .Build(ref state);
        }

        public void OnUpdate(ref SystemState state, UpdateTimeJob job = default)
        {
            _onHandle.Update(ref state);
            _remainingHandle.Update(ref state);
            _activeHandle.Update(ref state);
            _durationHandle.Update(ref state);

            job.OnHandle = _onHandle;
            job.RemainingHandle = _remainingHandle;
            job.ActiveHandle = _activeHandle;
            job.DurationHandle = _durationHandle;
            job.DeltaTime = state.WorldUnmanaged.Time.DeltaTime;
            job.SystemVersion = state.LastSystemVersion;

            state.Dependency = job.ScheduleParallel(_query, state.Dependency);
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

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var activeChanged = chunk.DidChange(ref ActiveHandle, SystemVersion);

                if (activeChanged)
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref RemainingHandle);
                    var durations = (float*)chunk.GetRequiredComponentDataPtrRO(ref DurationHandle);
                    var durationOnBits = chunk.GetRequiredEnabledBitsRO(ref OnHandle);
                    var triggerBits = chunk.GetRequiredEnabledBitsRO(ref ActiveHandle);
                    var durationOns = (ulong*)&durationOnBits;
                    var triggers = (ulong*)&triggerBits;

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (BovineLabs.Core.Internal.Bitwise.IsSet(triggers, i) && !BovineLabs.Core.Internal.Bitwise.IsSet(durationOns, i))
                        {
                            remainings[i] = durations[i];
                        }
                    }
                }

                if (activeChanged || chunk.DidChange(ref RemainingHandle, SystemVersion))
                {
                    // We open RO to avoid change filter trigger unless it has changed
                    ref readonly var original = ref chunk.GetRequiredEnabledBitsRO(ref OnHandle);
                    var updated = original;
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref RemainingHandle);

                    CalculateOn(remainings, (ulong*)&updated, chunk.Count, DeltaTime);

                    var hasChanged = updated.ULong0 != original.ULong0 || updated.ULong1 != original.ULong1;

                    if (hasChanged)
                    {
                        ref var enabledBits = ref chunk.GetRequiredEnabledBitsRW(ref OnHandle, out var count);
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
