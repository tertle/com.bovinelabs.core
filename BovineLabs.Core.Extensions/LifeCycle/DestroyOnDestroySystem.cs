// <copyright file="DestroyOnDestroySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [UpdateInGroup(typeof(DestroySystemGroup), OrderFirst = true)]
    public partial struct DestroyOnDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new DestroyJob
                {
                    DestroyEntities = SystemAPI.GetComponentLookup<DestroyEntity>(),
                    LinkedEntityGroups = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                    EntityStorageInfoLookup = SystemAPI.GetEntityStorageInfoLookup(),
                }
                .ScheduleParallel();
        }

        [BurstCompile]
        [WithChangeFilter(typeof(DestroyEntity))]
        [WithAll(typeof(DestroyEntity))]
        private partial struct DestroyJob : IJobEntity
        {
            [NativeDisableParallelForRestriction]
            public ComponentLookup<DestroyEntity> DestroyEntities;

            [NativeDisableContainerSafetyRestriction]
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroups;

            [ReadOnly]
            public EntityStorageInfoLookup EntityStorageInfoLookup;

            private void Execute(DynamicBuffer<LinkedEntityGroup> linkedEntityGroup)
            {
                this.Destroy(linkedEntityGroup);
            }

            private void Destroy(DynamicBuffer<LinkedEntityGroup> linkedEntityGroup)
            {
                var leg = linkedEntityGroup.AsNativeArray();

                // i >= 1 so we ignore ourselves
                for (var i = leg.Length - 1; i >= 1; i--)
                {
                    var entity = leg[i].Value;

                    if (entity.Index < 0 || !this.EntityStorageInfoLookup.Exists(entity))
                    {
                        // Entity has already been destroyed, just safely handle it so we don't have to care about ownership here
                        linkedEntityGroup.RemoveAtSwapBack(i);
                        continue;
                    }

                    // Check child has destroy component, if not we just let regular destroy handle it
                    var enabled = this.DestroyEntities.GetComponentEnabledRefRWOptional<DestroyEntity>(entity);
                    if (!enabled.IsValid)
                    {
                        continue;
                    }

                    // Need to be removed from LEG so it can be handled by destroy system instead
                    linkedEntityGroup.RemoveAtSwapBack(i);

                    // Destroy already being handled, so we don't touch it as it will be iterated over at the top level
                    if (enabled.ValueRO)
                    {
                        continue;
                    }

                    enabled.ValueRW = true;

                    // Propagate down
                    if (this.LinkedEntityGroups.TryGetBuffer(entity, out var newLinkedEntityGroup))
                    {
                        this.Destroy(newLinkedEntityGroup);
                    }
                }
            }
        }
    }
}
#endif
