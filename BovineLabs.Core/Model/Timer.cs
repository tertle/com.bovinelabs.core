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
        private ComponentTypeHandle<TOn> _onHandle;
        private ComponentTypeHandle<TRemaining> _remainingHandle;
        private ComponentTypeHandle<TActive> _activeHandle;
        private ComponentTypeHandle<TDuration> _durationHandle;
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            Assert.AreEqual(UnsafeUtility.SizeOf<bool>(), UnsafeUtility.SizeOf<TOn>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TRemaining>());
            Assert.AreEqual(UnsafeUtility.SizeOf<float>(), UnsafeUtility.SizeOf<TDuration>());

            _onHandle = state.GetComponentTypeHandle<TOn>();
            _remainingHandle = state.GetComponentTypeHandle<TRemaining>();
            _activeHandle = state.GetComponentTypeHandle<TActive>(true);
            _durationHandle = state.GetComponentTypeHandle<TDuration>(true);

            _query = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<TRemaining, TOn>()
                .WithAll<TActive, TDuration>()
                .WithOptions(EntityQueryOptions.FilterWriteGroup)
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

            [NativeDisableContainerSafetyRestriction] // Only initialized in the job
            private NativeList<bool> _onBuffer;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var activeChanged = chunk.DidChange(ref ActiveHandle, SystemVersion);

                if (activeChanged)
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref RemainingHandle);
                    var triggers = (bool*)chunk.GetRequiredComponentDataPtrRO(ref ActiveHandle);
                    var durations = (float*)chunk.GetRequiredComponentDataPtrRO(ref DurationHandle);
                    var durationOns = (bool*)chunk.GetRequiredComponentDataPtrRO(ref OnHandle);

                    for (var i = 0; i < chunk.Count; i++)
                    {
                        if (triggers[i] && !durationOns[i])
                        {
                            remainings[i] = durations[i];
                        }
                    }
                }

                if (activeChanged || chunk.DidChange(ref RemainingHandle, SystemVersion))
                {
                    var remainings = (float*)chunk.GetRequiredComponentDataPtrRW(ref RemainingHandle);

                    if (!_onBuffer.IsCreated)
                    {
                        _onBuffer = new NativeList<bool>(chunk.Count, Allocator.Temp);
                    }

                    _onBuffer.ResizeUninitialized(chunk.Count);
                    CalculateOnVectorized(remainings, _onBuffer.GetUnsafeReadOnlyPtr(), _onBuffer.Length, DeltaTime);

                    // We open RO to avoid change filter trigger unless it has changed
                    var original = chunk.GetRequiredComponentDataPtrRO(ref OnHandle);
                    var updated = _onBuffer.GetUnsafeReadOnlyPtr();
                    var hasChanged = UnsafeUtility.MemCmp(original, updated, UnsafeUtility.SizeOf<bool>() * _onBuffer.Length) != 0;

                    if (hasChanged)
                    {
                        var ons = chunk.GetNativeArray(ref OnHandle).Reinterpret<bool>();
                        ons.CopyFrom(_onBuffer.AsArray());
                    }
                }
            }

            private static void CalculateOnVectorized([NoAlias] float* remainings, [NoAlias] bool* isOn, int length, float deltaTime)
            {
                for (var i = 0; i < length; i++)
                {
// #if UNITY_BURST_EXPERIMENTAL_LOOP_INTRINSICS
//                     Loop.ExpectVectorized();
// #endif
                    remainings[i] = math.max(0, remainings[i] - deltaTime);
                    isOn[i] = remainings[i] != 0;
                }
            }
        }
    }
}
