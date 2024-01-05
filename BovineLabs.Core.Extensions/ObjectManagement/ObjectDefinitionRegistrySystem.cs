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
    using UnityEngine;

    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial struct ObjectDefinitionRegistrySystem : ISystem
    {
        private NativeHashMap<int, int> objectDefinitionsOffsets;
        private NativeList<Entity> objectDefinitions;
        private EntityQuery newQuery;
        private EntityQuery oldQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.objectDefinitionsOffsets = new NativeHashMap<int, int>(0, Allocator.Persistent);
            this.objectDefinitions = new NativeList<Entity>(0, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new ObjectDefinitionRegistry(this.objectDefinitions, this.objectDefinitionsOffsets));

            this.newQuery = SystemAPI.QueryBuilder().WithAll<Mod, ObjectDefinitionSetupRegistry>().WithNone<Initialized>().Build();
            this.oldQuery = SystemAPI.QueryBuilder().WithNone<Mod, ObjectDefinitionSetupRegistry>().WithAll<Initialized>().Build();

            var anyQueries = new NativeArray<EntityQuery>(2, Allocator.Temp);
            anyQueries[0] = this.newQuery;
            anyQueries[1] = this.oldQuery;

            state.RequireAnyForUpdate(anyQueries);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.objectDefinitionsOffsets.Dispose();
            this.objectDefinitions.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            state.EntityManager.RemoveComponent<Initialized>(this.oldQuery);
            state.EntityManager.AddComponent<Initialized>(this.newQuery);

            this.objectDefinitions.Clear();
            this.objectDefinitionsOffsets.Clear();

            SystemAPI.GetSingletonRW<ObjectDefinitionRegistry>(); // Trigger change filter

            var offsets = 0;
            foreach (var (mod, odr) in SystemAPI.Query<Mod, DynamicBuffer<ObjectDefinitionSetupRegistry>>())
            {
                if (!this.objectDefinitionsOffsets.TryAdd(mod.Value, offsets))
                {
                    Debug.LogError($"Mod with key {mod.Value} already added, skipping duplicate. Inform author of collision.");
                    continue;
                }

                offsets += odr.Length;
                this.objectDefinitions.AddRange(odr.AsNativeArray().Reinterpret<Entity>());
            }
        }

        internal struct Initialized : ICleanupComponentData
        {
        }
    }
}
#endif
