// <copyright file="ObjectDefinitionRegistrySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial struct ObjectDefinitionRegistrySystem : ISystem
    {
        private NativeHashMap<ObjectId, Entity> objectDefinitions;
        private EntityQuery newQuery;
        private EntityQuery oldQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.objectDefinitions = new NativeHashMap<ObjectId, Entity>(0, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new ObjectDefinitionRegistry(this.objectDefinitions));

            this.newQuery = SystemAPI.QueryBuilder().WithAll<ObjectDefinitionSetupRegistry>().WithNone<Initialized>().Build();
            this.oldQuery = SystemAPI.QueryBuilder().WithNone<ObjectDefinitionSetupRegistry>().WithAll<Initialized>().Build();

            var anyQueries = new NativeArray<EntityQuery>(2, Allocator.Temp);
            anyQueries[0] = this.newQuery;
            anyQueries[1] = this.oldQuery;

            state.RequireAnyForUpdate(anyQueries);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.objectDefinitions.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            state.EntityManager.RemoveComponent<Initialized>(this.oldQuery);
            state.EntityManager.AddComponent<Initialized>(this.newQuery);

            this.objectDefinitions.Clear();

            SystemAPI.GetSingletonRW<ObjectDefinitionRegistry>(); // Trigger change filter
            var blDebug = SystemAPI.GetSingleton<BLLogger>();

            foreach (var objects in SystemAPI.Query<DynamicBuffer<ObjectDefinitionSetupRegistry>>())
            {
                foreach (var obj in objects)
                {
                    if (!this.objectDefinitions.TryAdd(obj.Id, obj.Prefab))
                    {
                        blDebug.LogError512($"Asset {obj.Id.ToFixedString()} already added, skipping duplicate. Inform author of collision.");
                    }
                }
            }
        }

        internal struct Initialized : ICleanupComponentData
        {
        }
    }
}
#endif
