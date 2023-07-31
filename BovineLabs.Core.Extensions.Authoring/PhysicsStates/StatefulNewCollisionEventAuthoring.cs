// <copyright file="StatefulNewCollisionEventAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.Authoring.PhysicsStates
{
    using BovineLabs.Core.PhysicsStates;
    using Unity.Entities;
    using UnityEngine;

    public class StatefulNewCollisionEventAuthoring : MonoBehaviour
    {
        public bool EventDetails;

        private class Baker : Baker<StatefulNewCollisionEventAuthoring>
        {
            public override void Bake(StatefulNewCollisionEventAuthoring authoring)
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
