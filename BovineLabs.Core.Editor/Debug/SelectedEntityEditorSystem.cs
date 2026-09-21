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

        private NativeList<Entity> _entities;

        private NativeList<EntityId> _instanceIds;
        private NativeParallelMultiHashMap<EntityId, Entity> _entityLookup;

        private JobHandle _lastFrame;

        protected override void OnCreate()
        {
            _entities = new NativeList<Entity>(512, Allocator.Persistent);

            _instanceIds = new NativeList<EntityId>(512, Allocator.Persistent);
            _entityLookup = new NativeParallelMultiHashMap<EntityId, Entity>(1024, Allocator.Persistent);

            EntityManager.CreateEntity<SelectedEntity, SelectedEntities>("Selected Entity");
        }

        protected override void OnDestroy()
        {
            _instanceIds.Dispose();
            _entities.Dispose();
            _entityLookup.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!IsEnabled.Data)
            {
                return;
            }

            _lastFrame.Complete();
            _instanceIds.Clear();
            _entities.Clear();

            var selectedEntities = SystemAPI.QueryBuilder().WithAllRW<SelectedEntities>().Build().GetSingletonBufferNoSync<SelectedEntities>(false);

            EntitySelection.GetAllSelectionsInWorld(World, _entities, _instanceIds);

            // No need to build this if not selecting a gameobject
            if (_instanceIds.Length > 0)
            {
                var query = SystemAPI
                    .QueryBuilder()
                    .WithAll<EntityGuid>()
                    .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
                    .Build();

                var count = query.CalculateEntityCount();

                Dependency = new ResizeJob
                {
                    EntityLookup = _entityLookup,
                    Count = count,
                }.Schedule(Dependency);

                Dependency = new BuildInstanceIDToEntityIndexJob
                {
                    EntityLookup = _entityLookup.AsParallelWriter(),
                    GuidType = SystemAPI.GetComponentTypeHandle<EntityGuid>(true),
                    EntityType = SystemAPI.GetEntityTypeHandle(),
                }.ScheduleParallel(query, Dependency);
            }

            Dependency = new SetSelectionJob
            {
                EntityLookup = _entityLookup,
                InstanceIDs = _instanceIds,
                Entities = _entities,
                EntityGuids = SystemAPI.GetComponentLookup<EntityGuid>(true),
                SelectedEntitys = SystemAPI.GetComponentLookup<SelectedEntity>(),
                SelectedEntities = selectedEntities,
                SingletonEntity = SystemAPI.GetSingletonEntity<SelectedEntity>(),
            }.Schedule(Dependency);

            _lastFrame = Dependency;
        }

        [BurstCompile]
        private struct ResizeJob : IJob
        {
            public NativeParallelMultiHashMap<EntityId, Entity> EntityLookup;
            public int Count;

            public void Execute()
            {
                EntityLookup.Clear();
                if (EntityLookup.Capacity < Count)
                {
                    EntityLookup.Capacity = Count;
                }
            }
        }

        [BurstCompile]
        private struct BuildInstanceIDToEntityIndexJob : IJobChunk
        {
            public NativeParallelMultiHashMap<EntityId, Entity>.ParallelWriter EntityLookup;

            [ReadOnly]
            public ComponentTypeHandle<EntityGuid> GuidType;

            [ReadOnly]
            public EntityTypeHandle EntityType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityType);
                var guids = chunk.GetNativeArray(ref GuidType).Slice()
                    .SliceWithStride<EntityId>();
                EntityLookup.AddBatchUnsafe(guids, entities);
            }
        }

        [BurstCompile]
        private struct SetSelectionJob : IJob
        {
            [ReadOnly]
            public NativeList<EntityId> InstanceIDs;
            [ReadOnly]
            public NativeParallelMultiHashMap<EntityId, Entity> EntityLookup;

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
                SelectedEntities.Clear();

                foreach (var entity in Entities)
                {
                    if (selectedEntity.Value == Entity.Null)
                    {
                        selectedEntity.Value = entity;
                    }

                    SelectedEntities.Add(new SelectedEntities { Value = entity });
                }

                foreach (var instanceID in InstanceIDs)
                {
                    if (EntityLookup.TryGetFirstValue(instanceID, out var entity, out var it))
                    {
                        do
                        {
                            if (EntityGuids[entity].Serial == 0)
                            {
                                if (selectedEntity.Value == Entity.Null)
                                {
                                    selectedEntity.Value = entity;
                                }

                                SelectedEntities.Add(new SelectedEntities { Value = entity });
                                break;
                            }
                        }
                        while (EntityLookup.TryGetNextValue(out entity, ref it));
                    }
                }

                SelectedEntitys[SingletonEntity] = selectedEntity;
            }
        }
    }
}
