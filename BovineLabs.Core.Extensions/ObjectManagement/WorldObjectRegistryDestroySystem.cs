// <copyright file="WorldObjectRegistryDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION && !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.LifeCycle;
    using Unity.Burst;
    using Unity.Burst.Intrinsics;
    using Unity.Collections;
    using Unity.Entities;

    [WorldSystemFilter(Worlds.Simulation)]
    [UpdateInGroup(typeof(DestroySystemGroup))]
    public partial struct WorldObjectRegistryDestroySystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var registry = SystemAPI.QueryBuilder().WithAllRW<WorldObjectRegistry>().Build().GetSingletonBufferNoSync<WorldObjectRegistry>(false);
            var query = SystemAPI.QueryBuilder().WithAll<ObjectId, DestroyEntity>().Build();

            state.Dependency = new UnregisterWorldObjectJob
            {
                Registry = registry,
                EntityHandle = SystemAPI.GetEntityTypeHandle(),
                ObjectIdHandle = SystemAPI.GetComponentTypeHandle<ObjectId>(true),
            }.Schedule(query, state.Dependency);
        }

        [BurstCompile]
        private struct UnregisterWorldObjectJob : IJobChunk
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
                    registry.Remove(objectId, entities[entityIndex]);
                }
            }
        }
    }
}
#endif
