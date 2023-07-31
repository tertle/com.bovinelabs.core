// <copyright file="StatefulCollisionEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;
    using Unity.Properties;

    [InternalBufferCapacity(0)]
    public struct StatefulCollisionEvent : IBufferElementData, ISimulationEvent<StatefulCollisionEvent>
    {
        internal StatefulCollisionEvent(StatefulCollisionEventContainer collisionEvent, StatefulEventState state)
        {
            this.EntityA = collisionEvent.CollisionEventData.Entities.EntityA;
            this.EntityB = collisionEvent.CollisionEventData.Entities.EntityB;
            this.BodyIndexA = collisionEvent.CollisionEventData.BodyIndices.BodyIndexA;
            this.BodyIndexB = collisionEvent.CollisionEventData.BodyIndices.BodyIndexB;
            this.ColliderKeyA = collisionEvent.CollisionEventData.ColliderKeys.ColliderKeyA;
            this.ColliderKeyB = collisionEvent.CollisionEventData.ColliderKeys.ColliderKeyB;
            this.Normal = collisionEvent.CollisionEventData.Normal;
            this.State = state;
            this.CollisionDetails = default;
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

        [CreateProperty]
        public float3 Normal { get; set; }

        // Only if CalculateDetails is checked on PhysicsCollisionEventBuffer of selected entity,
        // this field will have valid value, otherwise it will be zero initialized
        [CreateProperty]
        internal Details CollisionDetails { get; set;  }

        public bool TryGetDetails(out Details details)
        {
            details = this.CollisionDetails;
            return this.CollisionDetails.IsValid;
        }

        public bool Equals(StatefulCollisionEvent other)
        {
            return this.EntityA.Equals(other.EntityA) &&
                   this.EntityB.Equals(other.EntityB) &&
                   this.ColliderKeyA.Equals(other.ColliderKeyA) &&
                   this.ColliderKeyB.Equals(other.ColliderKeyB);
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

        public int CompareTo(StatefulCollisionEvent other)
        {
            return ISimulationEventUtilities.CompareEvents(this, other);
        }

        // This struct describes additional, optional, details about collision of 2 bodies
        public readonly struct Details
        {
            internal readonly bool IsValid;

            /// <summary>
            /// If 1, then it is a vertex collision
            /// If 2, then it is an edge collision
            /// If 3 or more, then it is a face collision
            /// </summary>
            public readonly int NumberOfContactPoints;

            // Estimated impulse applied
            public readonly float EstimatedImpulse;

            // Average contact point position
            public readonly float3 AverageContactPointPosition;

            public Details(int numContactPoints, float estimatedImpulse, float3 averageContactPosition)
            {
                this.IsValid = numContactPoints > 0;
                this.NumberOfContactPoints = numContactPoints;
                this.EstimatedImpulse = estimatedImpulse;
                this.AverageContactPointPosition = averageContactPosition;
            }
        }
    }
}
#endif
