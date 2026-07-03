// <copyright file="WorldObjectRegistryCreateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION && !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.LifeCycle;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.Simulation)]
    [UpdateInGroup(typeof(InitializeSystemGroup))]
    public partial struct WorldObjectRegistryCreateSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var entity = state.EntityManager.CreateSingletonBuffer<WorldObjectRegistry>("World Object Registry");
            state.EntityManager.GetBuffer<WorldObjectRegistry>(entity).InitializeMultiHashMap<WorldObjectRegistry, ObjectId, Entity>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.QueryBuilder().WithAllRW<WorldObjectRegistry>().Build().GetSingletonBufferNoSync<WorldObjectRegistry>(false);
            var query = SystemAPI.QueryBuilder().WithAll<ObjectId>().WithAny<InitializeEntity, InitializeSubSceneEntity>().Build();

            state.Dependency = new RegisterWorldObjectJob
            {
                Registry = registry,
                EntityHandle = SystemAPI.GetEntityTypeHandle(),
                ObjectIdHandle = SystemAPI.GetComponentTypeHandle<ObjectId>(true),
            }.Schedule(query, state.Dependency);
        }

        [BurstCompile]
        private struct RegisterWorldObjectJob : IJobChunk
        {
            public DynamicBuffer<WorldObjectRegistry> Registry;

            [ReadOnly]
            public EntityTypeHandle EntityHandle;

            [ReadOnly]
            public ComponentTypeHandle<ObjectId> ObjectIdHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var registry = this.Registry.AsMap();
                var entities = chunk.GetNativeArray(this.EntityHandle);
                var objectIds = chunk.GetNativeArray(ref this.ObjectIdHandle);

                var e = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (e.NextEntityIndex(out var entityIndex))
                {
                    var objectId = objectIds[entityIndex];
                    Check.Assume(objectId != ObjectId.Null, "Object somehow has Null id");
                    registry.Add(objectId, entities[entityIndex]);
                }
            }
        }
    }
}
#endif
