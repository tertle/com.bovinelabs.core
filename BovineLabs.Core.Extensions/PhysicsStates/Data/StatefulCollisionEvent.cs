// <copyright file="StatefulCollisionEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Physics;

    [InternalBufferCapacity(0)]
    public struct StatefulCollisionEvent : IBufferElementData
    {
        public Entity EntityB;
        public int BodyIndexA;
        public int BodyIndexB;
        public ColliderKey ColliderKeyA;
        public ColliderKey ColliderKeyB;
        public StatefulEventState State;
        public float3 Normal;

        // Only if CalculateDetails is checked on PhysicsCollisionEventBuffer of selected entity,
        // this field will have valid value, otherwise it will be zero initialized
        public Details CollisionDetails;

        public bool TryGetDetails(out Details details)
        {
            details = this.CollisionDetails;
            return this.CollisionDetails.IsValid;
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
