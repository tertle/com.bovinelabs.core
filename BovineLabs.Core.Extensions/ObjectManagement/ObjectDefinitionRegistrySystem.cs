// <copyright file="ObjectDefinitionRegistrySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEngine;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial struct ObjectDefinitionRegistrySystem : ISystem
    {
        private NativeHashMap<int, int> objectDefinitionsOffsets;
        private NativeList<Entity> objectDefinitions;
        private EntityQuery newQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.objectDefinitionsOffsets = new NativeHashMap<int, int>(0, Allocator.Persistent);
            this.objectDefinitions = new NativeList<Entity>(0, Allocator.Persistent);

            state.EntityManager.AddComponentData(state.SystemHandle, new ObjectDefinitionRegistry(this.objectDefinitions, this.objectDefinitionsOffsets));

            this.newQuery = SystemAPI.QueryBuilder().WithAll<Mod, ObjectDefinitionSetupRegistry>().WithAll<ObjectDefinitionSetupRegistryInitialized>().Build();
            state.RequireForUpdate(this.newQuery);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.objectDefinitionsOffsets.Dispose();
            this.objectDefinitions.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

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

            state.EntityManager.RemoveComponent<ObjectDefinitionSetupRegistryInitialized>(this.newQuery);
        }
    }
}
