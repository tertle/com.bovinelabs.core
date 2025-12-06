// <copyright file="FixedStepUpdatedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_ALWAYS_UPDATE && UNITY_PHYSICS
namespace BovineLabs.Core.PhysicsUpdate
{
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Physics.Systems;

    [UpdateInGroup(typeof(PhysicsInitializeGroup))]
    public partial struct FixedStepUpdatedSystem : ISystem
    {
        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<AlwaysUpdatePhysicsWorld>(state.SystemHandle);
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.SetSingleton(new AlwaysUpdatePhysicsWorld { FixedStepUpdatedThisFrame = true });
        }
    }
}
#endif
