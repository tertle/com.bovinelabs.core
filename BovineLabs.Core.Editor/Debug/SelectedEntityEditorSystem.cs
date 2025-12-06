// <copyright file="SelectedEntityEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Internal;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;
    using UnityEngine;

    [Configurable]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntityEditorSystem : SystemBase
    {
        [ConfigVar("debug.selection", true, "Write the current hierarchy selection to SelectedEntity and SelectedEntities.")]
        public static readonly SharedStatic<bool> IsEnabled = SharedStatic<bool>.GetOrCreate<SelectedEntityEditorSystem>();

        private NativeList<Entity> entities;

#if UNITY_6000_4_OR_NEWER
        private NativeList<EntityId> instanceIds;
        private NativeParallelMultiHashMap<EntityId, Entity> entityLookup;
#else
        private NativeList<int> instanceIds;
        private NativeParallelMultiHashMap<int, Entity> entityLookup;
#endif

        private JobHandle lastFrame;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.entities = new NativeList<Entity>(512, Allocator.Persistent);

#if UNITY_6000_4_OR_NEWER
            this.instanceIds = new NativeList<EntityId>(512, Allocator.Persistent);
            this.entityLookup = new NativeParallelMultiHashMap<EntityId, Entity>(1024, Allocator.Persistent);
#else
            this.instanceIds = new NativeList<int>(512, Allocator.Persistent);
            this.entityLookup = new NativeParallelMultiHashMap<int, Entity>(1024, Allocator.Persistent);
#endif

            this.EntityManager.CreateEntity(typeof(SelectedEntity), typeof(SelectedEntities));
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            this.instanceIds.Dispose();
            this.entities.Dispose();
            this.entityLookup.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            if (!IsEnabled.Data)
            {
                return;
            }

            this.lastFrame.Complete();
            this.instanceIds.Clear();
            this.entities.Clear();

            var selectedEntities = SystemAPI.QueryBuilder().WithAllRW<SelectedEntities>().Build().GetSingletonBufferNoSync<SelectedEntities>(false);

            EntitySelection.GetAllSelectionsInWorld(this.World, this.entities, this.instanceIds);

            // No need to build this if not selecting a gameobject
            if (this.instanceIds.Length > 0)
            {
                var query = SystemAPI
                    .QueryBuilder()
                    .WithAll<EntityGuid>()
                    .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
                    .Build();

                var count = query.CalculateEntityCount();

                this.Dependency = new ResizeJob
                {
                    EntityLookup = this.entityLookup,
                    Count = count,
                }.Schedule(this.Dependency);

                this.Dependency = new BuildInstanceIDToEntityIndexJob
                {
                    EntityLookup = this.entityLookup.AsParallelWriter(),
                    GuidType = SystemAPI.GetComponentTypeHandle<EntityGuid>(true),
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                }.ScheduleParallel(query, this.Dependency);
            }

            this.Dependency = new SetSelectionJob
            {
                EntityLookup = this.entityLookup,
                InstanceIDs = this.instanceIds,
                Entities = this.entities,
                EntityGuids = SystemAPI.GetComponentLookup<EntityGuid>(true),
                SelectedEntitys = SystemAPI.GetComponentLookup<SelectedEntity>(),
                SelectedEntities = selectedEntities,
                SingletonEntity = SystemAPI.GetSingletonEntity<SelectedEntity>(),
            }.Schedule(this.Dependency);

            this.lastFrame = this.Dependency;
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
#if UNITY_6000_4_OR_NEWER
            public NativeParallelMultiHashMap<EntityId, Entity> EntityLookup;
#else
            public NativeParallelMultiHashMap<int, Entity> EntityLookup;
#endif
            public int Count;

            public void Execute()
            {
                this.EntityLookup.Clear();
                if (this.EntityLookup.Capacity < this.Count)
                {
                    this.EntityLookup.Capacity = this.Count;
                }
            }
        }

        [BurstCompile]
        private struct BuildInstanceIDToEntityIndexJob : IJobChunk
        {
#if UNITY_6000_4_OR_NEWER
            public NativeParallelMultiHashMap<EntityId, Entity>.ParallelWriter EntityLookup;
#else
            public NativeParallelMultiHashMap<int, Entity>.ParallelWriter EntityLookup;
#endif

            [ReadOnly]
            public ComponentTypeHandle<EntityGuid> GuidType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(this.EntityType);
                var guids = chunk.GetNativeArray(ref this.GuidType).Slice()
#if UNITY_6000_4_OR_NEWER
                    .SliceWithStride<EntityId>();
#else
                    .SliceWithStride<int>();
#endif
                this.EntityLookup.AddBatchUnsafe(guids, entities);
            }
        }

        [BurstCompile]
        private struct SetSelectionJob : IJob
        {
#if UNITY_6000_4_OR_NEWER
            [ReadOnly]
            public NativeList<EntityId> InstanceIDs;
            [ReadOnly]
            public NativeParallelMultiHashMap<EntityId, Entity> EntityLookup;
#else
            [ReadOnly]
            public NativeList<int> InstanceIDs;
            [ReadOnly]
            public NativeParallelMultiHashMap<int, Entity> EntityLookup;
#endif

            [ReadOnly]
            public NativeList<Entity> Entities;

            [ReadOnly]
            public ComponentLookup<EntityGuid> EntityGuids;

            public ComponentLookup<SelectedEntity> SelectedEntitys;

            public DynamicBuffer<SelectedEntities> SelectedEntities;

            public Entity SingletonEntity;

            public void Execute()
            {
                var selectedEntity = default(SelectedEntity);
                this.SelectedEntities.Clear();

                foreach (var entity in this.Entities)
                {
                    if (selectedEntity.Value == Entity.Null)
                    {
                        selectedEntity.Value = entity;
                    }

                    this.SelectedEntities.Add(new SelectedEntities { Value = entity });
                }

                foreach (var instanceID in this.InstanceIDs)
                {
                    if (this.EntityLookup.TryGetFirstValue(instanceID, out var entity, out var it))
                    {
                        do
                        {
                            if (this.EntityGuids[entity].Serial == 0)
                            {
                                if (selectedEntity.Value == Entity.Null)
                                {
                                    selectedEntity.Value = entity;
                                }

                                this.SelectedEntities.Add(new SelectedEntities { Value = entity });
                                break;
                            }
                        }
                        while (this.EntityLookup.TryGetNextValue(out entity, ref it));
                    }
                }

                this.SelectedEntitys[this.SingletonEntity] = selectedEntity;
            }
        }
    }
}
