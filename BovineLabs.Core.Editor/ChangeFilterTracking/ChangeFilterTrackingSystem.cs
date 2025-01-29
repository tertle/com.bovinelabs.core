// <copyright file="ChangeFilterTrackingSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ChangeFilterTracking
{
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    [Configurable]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ChangeFilterTrackingSystem : ISystem
    {
        private const int ShortUpdateTime = 60; // must be multiple of FramesToTrack
        private const int FramesToTrack = 600;

        [ConfigVar("debug.changefilter.enabled", true, "Enable change filter tracking.")]
        internal static readonly SharedStatic<bool> IsEnabled = SharedStatic<bool>.GetOrCreate<IsEnabledContext>();

        [ConfigVar("debug.changefilter.threshold", 0.85f, "Warn threshold.")]
        internal static readonly SharedStatic<float> WarningLevel = SharedStatic<float>.GetOrCreate<WarningLevelContext>();

        private NativeArray<JobHandle> jobHandles;
        private NativeArray<TypeTrack> typeTracks;
        private int frameIndex;

        public NativeArray<TypeTrack> TypeTracks => this.typeTracks;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            var typeIndices = new List<ComponentType>();

            foreach (var typeInfo in TypeManager.AllTypes)
            {
                if (typeInfo.Category is not TypeManager.TypeCategory.ComponentData and not TypeManager.TypeCategory.BufferData)
                {
                    continue;
                }

                if (typeInfo.TypeIndex.IsManagedComponent)
                {
                    continue;
                }

                var type = typeInfo.Type;

                var attribute = type?.GetCustomAttribute<ChangeFilterTrackingAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                typeIndices.Add(ComponentType.ReadOnly(typeInfo.TypeIndex));
            }

            this.typeTracks = new NativeArray<TypeTrack>(typeIndices.Count, Allocator.Persistent);
            this.jobHandles = new NativeArray<JobHandle>(typeIndices.Count, Allocator.Persistent);

            for (var i = 0; i < typeIndices.Count; i++)
            {
                var componentType = typeIndices[i];

                this.typeTracks[i] = new TypeTrack
                {
                    TypeName = TypeManagerEx.GetTypeName(componentType.TypeIndex),
                    DynamicTypeHandle = state.GetDynamicComponentTypeHandle(componentType),
                    Query = state.GetEntityQuery(componentType),
                    Changed = new NativeArray<int>(FramesToTrack, Allocator.Persistent),
                    Chunks = new NativeArray<int>(FramesToTrack, Allocator.Persistent),
                    Result = new NativeArray<float>(FramesToTrack, Allocator.Persistent),
                    HasWarned = new NativeReference<bool>(Allocator.Persistent),
                    Short = new NativeReference<float>(Allocator.Persistent),
                    Long = new NativeReference<float>(Allocator.Persistent),
                };
            }

            if (typeIndices.Count == 0)
            {
                state.Enabled = false;
            }
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            for (var index = 0; index < this.typeTracks.Length; index++)
            {
                var tracked = this.typeTracks[index];
                tracked.Changed.Dispose();
                tracked.Chunks.Dispose();
                tracked.Result.Dispose();
                tracked.HasWarned.Dispose();
                tracked.Short.Dispose();
                tracked.Long.Dispose();
            }

            this.typeTracks.Dispose();
            this.jobHandles.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!IsEnabled.Data)
            {
                return;
            }

            var currentFrameIndex = this.frameIndex;
            var debug = SystemAPI.GetSingleton<BLDebug>();

            for (var index = 0; index < this.typeTracks.Length; index++)
            {
                var track = this.typeTracks[index];
                track.DynamicTypeHandle.Update(ref state);

                this.jobHandles[index] = new DidChangeJob
                {
                    DynamicTypeHandle = track.DynamicTypeHandle,
                    Changed = track.Changed,
                    Chunks = track.Chunks,
                    Index = currentFrameIndex,
                    LastSystemVersion = state.LastSystemVersion,
                }.Schedule(track.Query, state.Dependency);

                this.jobHandles[index] = new ValidateChangeFilterJob
                {
                    Changed = track.Changed,
                    Chunks = track.Chunks,
                    Result = track.Result,
                    HasWarned = track.HasWarned,
                    TypeName = track.TypeName,
                    Short = track.Short,
                    Long = track.Long,
                    Index = currentFrameIndex,
                    Debug = debug,
                }.Schedule(this.jobHandles[index]);
            }

            state.Dependency = JobHandle.CombineDependencies(this.jobHandles);

            this.frameIndex = (this.frameIndex + 1) % FramesToTrack;
        }

        public struct TypeTrack
        {
            public FixedString128Bytes TypeName;
            public DynamicComponentTypeHandle DynamicTypeHandle;
            public EntityQuery Query;
            public NativeArray<int> Changed;
            public NativeArray<int> Chunks;
            public NativeArray<float> Result;
            public NativeReference<bool> HasWarned;
            public NativeReference<float> Short;
            public NativeReference<float> Long;
        }

        [BurstCompile]
        private struct DidChangeJob : IJobChunk
        {
            [ReadOnly]
            public FakeDynamicComponentTypeHandle DynamicTypeHandle;

            public NativeArray<int> Changed;

            public NativeArray<int> Chunks;

            public int Index;
            public uint LastSystemVersion;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var changed = chunk.DidChange(this.DynamicTypeHandle.TypeIndex, ref this.DynamicTypeHandle.TypeLookupCache, this.LastSystemVersion);
                this.Changed[this.Index] += changed ? 1 : 0;
                this.Chunks[this.Index] += 1;
            }
        }

        [BurstCompile]
        private struct ValidateChangeFilterJob : IJob
        {
            public NativeArray<int> Changed;

            public NativeArray<int> Chunks;

            public NativeArray<float> Result;

            public NativeReference<bool> HasWarned;

            public NativeReference<float> Short;

            public NativeReference<float> Long;

            public FixedString128Bytes TypeName;

            public int Index;

            public BLDebug Debug;

            public void Execute()
            {
                var changed = this.Changed[this.Index];
                var chunks = this.Chunks[this.Index];

                this.Changed[this.Index] = 0;
                this.Chunks[this.Index] = 0;

                if (chunks == 0)
                {
                    this.Result[this.Index] = 0;
                }
                else
                {
                    this.Result[this.Index] = changed / (float)chunks;
                }

                if ((this.Index + 1) % ShortUpdateTime == 0)
                {
                    var total = mathex.sum(this.Result.GetSubArray((this.Index - ShortUpdateTime) + 1, ShortUpdateTime));
                    var averageChange = total / ShortUpdateTime;
                    this.Short.Value = averageChange;
                }

                if (this.Index == this.Result.Length - 1)
                {
                    var total = mathex.sum(this.Result);
                    var averageChange = total / this.Result.Length;
                    this.Long.Value = averageChange;

                    if (!this.HasWarned.Value && averageChange > WarningLevel.Data)
                    {
                        var percent = (int)(averageChange * 100);
                        this.Debug.Warning512($"{this.TypeName} DidChange triggered on average {percent}% of chunks per frame");
                        this.HasWarned.Value = true;
                    }
                }
            }
        }

        private struct WarningLevelContext
        {
        }

        private struct IsEnabledContext
        {
        }
    }
}
