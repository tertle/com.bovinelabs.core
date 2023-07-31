// <copyright file="StatefulTriggerEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Properties;

    [InternalBufferCapacity(0)]
    public struct StatefulTriggerEvent : IBufferElementData, ISimulationEvent<StatefulTriggerEvent>
    {
        internal StatefulTriggerEvent(StatefulTriggerEventContainer triggerEvent, StatefulEventState state)
        {
            this.EntityA = triggerEvent.TriggerEvent.EntityA;
            this.EntityB = triggerEvent.TriggerEvent.EntityB;
            this.BodyIndexA = triggerEvent.TriggerEvent.BodyIndexA;
            this.BodyIndexB = triggerEvent.TriggerEvent.BodyIndexB;
            this.ColliderKeyA = triggerEvent.TriggerEvent.ColliderKeyA;
            this.ColliderKeyB = triggerEvent.TriggerEvent.ColliderKeyB;
            this.State = state;
        }

        [CreateProperty]
        public Entity EntityA { get; }

        [CreateProperty]
        public Entity EntityB { get; }

        [CreateProperty]
        public int BodyIndexA { get; }

        [CreateProperty]
        public int BodyIndexB { get; }

        [CreateProperty]
        public ColliderKey ColliderKeyA { get; }

        [CreateProperty]
        public ColliderKey ColliderKeyB { get; }

        [CreateProperty]
        public StatefulEventState State { get; }

        public bool Equals(StatefulTriggerEvent other)
        {
            return this.EntityA.Equals(other.EntityA) &&
                   this.EntityB.Equals(other.EntityB) &&
                   this.ColliderKeyA.Value.Equals(other.ColliderKeyA.Value) &&
                   this.ColliderKeyB.Value.Equals(other.ColliderKeyB.Value);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.EntityA.GetHashCode();
                hashCode = (hashCode * 397) ^ this.EntityB.GetHashCode();
                return hashCode;
            }
        }

        public int CompareTo(StatefulTriggerEvent other)
        {
            return ISimulationEventUtilities.CompareEvents(this, other);
        }
    }
}
#endif
