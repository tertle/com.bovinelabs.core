// <copyright file="PhysicsLayerUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Utility
{
    using Unity.Physics;
    using UnityEngine;

    public static class PhysicsLayerUtil
    {
        public static CollisionFilter ProduceCollisionFilter(UnityEngine.Collider collider, GameObject body)
        {
            // Declaring the dependency on the GameObject with GetLayer, so the baker rebakes if the layer changes
            var layer = body.layer;

            // create filter and assign layer of this collider
            var filter = new CollisionFilter {BelongsTo = 1u << layer};

            uint includeMask = 0u;
            // incorporate global layer collision matrix
            for (var i = 0; i < 32; ++i)
            {
                includeMask |= Physics.GetIgnoreLayerCollision(layer, i) ? 0 : 1u << i;
            }

            // Now incorporate the layer overrides.
            // The exclude layers take precedence over the include layers.

            includeMask |= (uint)collider.includeLayers.value;
            var excludeMask = (uint)collider.excludeLayers.value;

            // obtain rigid body if any, and incorporate its layer overrides
            var rigidBody = body.GetComponent<Rigidbody>();
            if (rigidBody)
            {
                includeMask |= (uint)rigidBody.includeLayers.value;
                excludeMask |= (uint)rigidBody.excludeLayers.value;
            }

            // apply exclude mask to include mask and set the final result
            includeMask &= ~excludeMask;

            filter.CollidesWith = includeMask;

            return filter;
        }
    }
}
#endif