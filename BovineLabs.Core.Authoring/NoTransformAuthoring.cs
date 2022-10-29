// <copyright file="NoTransformAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Added NoTransformAuthoring will ensure all Transform components are excluded from an entity and all its children. </summary>
    [DisallowMultipleComponent]
    public class NoTransformAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool removeFromChildren = true;

        public bool RemoveFromChildren => this.removeFromChildren;
    }

    public class NoTransformBaker : Baker<NoTransformAuthoring>
    {
        public override void Bake(NoTransformAuthoring authoring)
        {
            if (authoring.RemoveFromChildren)
            {
                var children = authoring.GetComponentsInChildren<Transform>();

                foreach (var child in children)
                {
                    this.GetEntity(child, TransformUsageFlags.ManualOverride);
                }
            }
            else
            {
                this.GetEntity(TransformUsageFlags.ManualOverride);
            }
        }
    }
}
