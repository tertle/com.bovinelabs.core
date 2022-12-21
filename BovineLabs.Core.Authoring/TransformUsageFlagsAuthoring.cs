// <copyright file="TransformUsageFlagsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Added NoTransformAuthoring will ensure all Transform components are excluded from an entity and all its children. </summary>
    [DisallowMultipleComponent]
    public class TransformUsageFlagsAuthoring : MonoBehaviour
    {
        public TransformUsageFlags TransformUsageFlags = TransformUsageFlags.Default;
        public bool SetInChildren = true;
    }

    public class TransformUsageFlagsAuthoringBaker : Baker<TransformUsageFlagsAuthoring>
    {
        public override void Bake(TransformUsageFlagsAuthoring usageFlagsAuthoring)
        {
            if (usageFlagsAuthoring.SetInChildren)
            {
                var children = usageFlagsAuthoring.GetComponentsInChildren<Transform>();

                foreach (var child in children)
                {
                    this.AddTransformUsageFlags(child, usageFlagsAuthoring.TransformUsageFlags);
                }
            }
            else
            {
                this.AddTransformUsageFlags(usageFlagsAuthoring.TransformUsageFlags);
            }
        }
    }
}
