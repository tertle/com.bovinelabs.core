// <copyright file="StatefulCollisionEventAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.Authoring.PhysicsStates
{
    using BovineLabs.Core.PhysicsStates;
    using Unity.Entities;
    using UnityEngine;

    public class StatefulCollisionEventAuthoring : MonoBehaviour
    {
        public bool EventDetails;

        private class Baker : Baker<StatefulCollisionEventAuthoring>
        {
            public override void Bake(StatefulCollisionEventAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddBuffer<StatefulCollisionEvent>(entity);

                if (authoring.EventDetails)
                {
                    this.AddComponent<StatefulCollisionEventDetails>(entity);
                }
            }
        }
    }
}
#endif
