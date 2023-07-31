// <copyright file="StatefulCollisionEventClearSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Physics.Systems;

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(StatefulCollisionEventSystem))]
    public partial struct StatefulCollisionEventClearSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ClearCollisionEventJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithChangeFilter(typeof(StatefulCollisionEvent))]
        public partial struct ClearCollisionEventJob : IJobEntity
        {
            public void Execute(ref DynamicBuffer<StatefulCollisionEvent> eventBuffer)
            {
                eventBuffer.Clear();
            }
        }
    }
}
#endif
