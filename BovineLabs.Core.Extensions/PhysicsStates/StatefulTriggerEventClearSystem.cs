// <copyright file="StatefulTriggerEventClearSystem.cs" company="BovineLabs">
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
    [UpdateBefore(typeof(StatefulTriggerEventSystem))]
    public partial struct StatefulTriggerEventClearSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ClearTriggerEventJob().ScheduleParallel();
        }

        [BurstCompile]
        [WithChangeFilter(typeof(StatefulTriggerEvent))]
        public partial struct ClearTriggerEventJob : IJobEntity
        {
            public void Execute(ref DynamicBuffer<StatefulTriggerEvent> eventBuffer)
            {
                eventBuffer.Clear();
            }
        }
    }
}
#endif
