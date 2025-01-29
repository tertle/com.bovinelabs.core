// <copyright file="StatefulCollisionEventContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using BovineLabs.Core.Assertions;
    using Unity.Entities;
    using Unity.Physics;

    internal readonly struct StatefulCollisionEventContainer : IEventContainer<StatefulCollisionEvent, StatefulCollisionEventContainer>
    {
        public readonly CollisionEventData CollisionEventData; // Only use this for values, not detail calculations
        public readonly CollisionEvent CollisionEvent; // Unsafe to use after frame it was used

        public StatefulCollisionEventContainer(CollisionEvent collisionEvent)
        {
            this.CollisionEventData = collisionEvent.EventData.Value;
            this.CollisionEvent = collisionEvent;
        }

        public Entity EntityA => this.CollisionEventData.Entities.EntityA;

        public Entity EntityB => this.CollisionEventData.Entities.EntityB;

        public StatefulCollisionEvent Create(Entity entity, StatefulEventState state)
        {
            if (this.CollisionEventData.Entities.EntityA.Equals(entity))
            {
                return new StatefulCollisionEvent
                {
                    EntityB = this.CollisionEventData.Entities.EntityB,
                    BodyIndexA = this.CollisionEventData.BodyIndices.BodyIndexA,
                    BodyIndexB = this.CollisionEventData.BodyIndices.BodyIndexB,
                    ColliderKeyA = this.CollisionEventData.ColliderKeys.ColliderKeyA,
                    ColliderKeyB = this.CollisionEventData.ColliderKeys.ColliderKeyB,
                    Normal = this.CollisionEventData.Normal,
                    State = state,
                    CollisionDetails = default,
                };
            }

            Check.Assume(this.CollisionEventData.Entities.EntityB.Equals(entity));

            return new StatefulCollisionEvent
            {
                EntityB = this.CollisionEventData.Entities.EntityA,
                BodyIndexA = this.CollisionEventData.BodyIndices.BodyIndexB,
                BodyIndexB = this.CollisionEventData.BodyIndices.BodyIndexA,
                ColliderKeyA = this.CollisionEventData.ColliderKeys.ColliderKeyB,
                ColliderKeyB = this.CollisionEventData.ColliderKeys.ColliderKeyA,
                Normal = this.CollisionEventData.Normal,
                State = state,
                CollisionDetails = default,
            };
        }

        public bool Equals(StatefulCollisionEventContainer other)
        {
            return this.CollisionEventData.Entities.EntityA.Equals(other.EntityA) &&
                this.CollisionEventData.Entities.EntityB.Equals(other.EntityB) &&
                this.CollisionEventData.ColliderKeys.ColliderKeyA.Equals(this.CollisionEventData.ColliderKeys.ColliderKeyA) &&
                this.CollisionEventData.ColliderKeys.ColliderKeyB.Equals(this.CollisionEventData.ColliderKeys.ColliderKeyB);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.CollisionEventData.Entities.EntityA.GetHashCode();
                hashCode = (hashCode * 397) ^ this.CollisionEventData.Entities.EntityB.GetHashCode();
                return hashCode;
            }
        }
    }
}
#endif
