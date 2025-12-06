// <copyright file="AlwaysUpdatePhysicsWorld.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_ALWAYS_UPDATE && UNITY_PHYSICS
namespace BovineLabs.Core.PhysicsUpdate
{
    using Unity.Entities;

    /// <summary>
    /// When FPS is greater than the tick rate of FixedStepSimulation (by default 60fps) this component continues to update the physics world.
    /// This ensures the spatial map is always up to date and can be reliably used inside of the regular update.
    /// It does not cause the physics world to be simulated.
    /// </summary>
    internal struct AlwaysUpdatePhysicsWorld : IComponentData
    {
        public bool FixedStepUpdatedThisFrame;
    }
}
#endif
