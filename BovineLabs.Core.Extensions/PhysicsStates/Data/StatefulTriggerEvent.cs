// <copyright file="StatefulTriggerEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Entities;
    using Unity.Physics;

    [InternalBufferCapacity(0)]
    public struct StatefulTriggerEvent : IBufferElementData
    {
        public Entity EntityB;
        public int BodyIndexA;
        public int BodyIndexB;
        public ColliderKey ColliderKeyA;
        public ColliderKey ColliderKeyB;
        public StatefulEventState State;
    }
}
#endif
