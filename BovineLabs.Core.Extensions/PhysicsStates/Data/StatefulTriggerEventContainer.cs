// <copyright file="StatefulTriggerEventContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Entities;
    using Unity.Physics;

    internal readonly struct StatefulTriggerEventContainer : IEventContainer<StatefulTriggerEvent, StatefulTriggerEventContainer>
    {
        public readonly TriggerEvent TriggerEvent;

        public StatefulTriggerEventContainer(TriggerEvent triggerEvent)
        {
            this.TriggerEvent = triggerEvent;
        }

        public Entity EntityA => this.TriggerEvent.EntityA;

        public Entity EntityB => this.TriggerEvent.EntityB;

        public StatefulTriggerEvent Create(StatefulEventState state)
        {
            return new StatefulTriggerEvent(this, state);
        }

        public bool Equals(StatefulTriggerEventContainer other)
        {
            return this.TriggerEvent.EntityA.Equals(other.EntityA) &&
                   this.TriggerEvent.EntityB.Equals(other.EntityB) &&
                   this.TriggerEvent.ColliderKeyA.Equals(this.TriggerEvent.ColliderKeyA) &&
                   this.TriggerEvent.ColliderKeyB.Equals(this.TriggerEvent.ColliderKeyB);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.TriggerEvent.EntityA.GetHashCode();
                hashCode = (hashCode * 397) ^ this.TriggerEvent.EntityB.GetHashCode();
                return hashCode;
            }
        }
    }
}
#endif
