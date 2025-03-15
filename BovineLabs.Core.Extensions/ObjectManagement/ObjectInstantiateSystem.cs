// <copyright file="ObjectInstantiateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Transforms;

    [WorldSystemFilter(Worlds.ServerLocalEditor)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public unsafe partial struct ObjectInstantiateSystem : ISystem
    {
        private EntityQuery query;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.query = new EntityQueryBuilder(state.WorldUpdateAllocator)
                .WithAll<ObjectInstantiate, LocalToWorld>()
                .WithNone<Initialized>()
                .WithNone<EntityGuid>() // Open subscenes
                .Build(ref state);

            state.RequireForUpdate(this.query);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var instantiateCount = this.query.CalculateEntityCount();
            var instantiate = new NativeParallelMultiHashMap<Entity, Ptr<LocalToWorld>>(instantiateCount, state.WorldUpdateAllocator);

            state.Dependency = new InstantiateJob
            {
                ToInstantiate = instantiate.AsParallelWriter(),
                ObjectInstantiateHandle = SystemAPI.GetComponentTypeHandle<ObjectInstantiate>(true),
                LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
            }.ScheduleParallel(this.query, state.Dependency);

            state.Dependency.Complete();

            var keys = instantiate.GetUniqueKeyArray(state.WorldUpdateAllocator);
            var instances = CollectionHelper.CreateNativeArray<Ptr<Entity>>(keys.Item2, state.WorldUpdateAllocator);

            state.EntityManager.AddComponent<Initialized>(this.query);

            for (var i = 0; i < keys.Item2; i++)
            {
                var prefab = keys.Item1[i];
                var prefabCount = instantiate.CountValuesForKey(prefab);

                var array = CollectionHelper.CreateNativeArray<Entity>(prefabCount, state.WorldUpdateAllocator);
                state.EntityManager.Instantiate(prefab, array);
                instances[i] = (Entity*)array.GetUnsafeReadOnlyPtr();
            }

            state.Dependency = new WriteTransformJob
            {
                Prefabs = keys.Item1,
                Instances = instances,
                Data = instantiate,
                LocalToWorlds = SystemAPI.GetComponentLookup<LocalToWorld>(),
                LocalTransforms = SystemAPI.GetComponentLookup<LocalTransform>(),
            }.ScheduleParallel(keys.Item2, 1, state.Dependency);
        }

        [BurstCompile]
        private struct InstantiateJob : IJobChunk
        {
            public NativeParallelMultiHashMap<Entity, Ptr<LocalToWorld>>.ParallelWriter ToInstantiate;

            [ReadOnly]
            public ComponentTypeHandle<ObjectInstantiate> ObjectInstantiateHandle;

            [ReadOnly]
            public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var objectInstantiates = (ObjectInstantiate*)chunk.GetRequiredComponentDataPtrRO(ref this.ObjectInstantiateHandle);
                var localToWorlds = (LocalToWorld*)chunk.GetRequiredComponentDataPtrRO(ref this.LocalToWorldHandle);

                // TODO we could batch add this to the hashmap
                for (var i = 0; i < chunk.Count; i++)
                {
                    var prefab = objectInstantiates[i].Prefab;
                    if (Hint.Unlikely(prefab == Entity.Null))
                    {
                        continue;
                    }

                    this.ToInstantiate.Add(prefab, localToWorlds + i);
                }
            }
        }

        [BurstCompile]
        private struct WriteTransformJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<Entity> Prefabs;

            [ReadOnly]
            public NativeArray<Ptr<Entity>> Instances;

            [ReadOnly]
            public NativeParallelMultiHashMap<Entity, Ptr<LocalToWorld>> Data;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalToWorld> LocalToWorlds;

            [NativeDisableParallelForRestriction]
            public ComponentLookup<LocalTransform> LocalTransforms;

            public void Execute(int index)
            {
                var prefab = this.Prefabs[index];
                var instances = this.Instances[index];

                var i = 0;
                this.Data.TryGetFirstValue(prefab, out var data, out var it);

                do
                {
                    var instance = instances.Value[i++];

                    if (Hint.Likely(this.LocalTransforms.TryGetRefRW(instance, out var lt)))
                    {
                        lt.ValueRW = LocalTransform.FromMatrix(data.Ref.Value);
                    }

                    this.LocalToWorlds[instance] = data.Ref;
                }
                while (this.Data.TryGetNextValue(out data, ref it));
            }
        }

        private struct Initialized : IComponentData
        {
        }
    }
}
#endif
