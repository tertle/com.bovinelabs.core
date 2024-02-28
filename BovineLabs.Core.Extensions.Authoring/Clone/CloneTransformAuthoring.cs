// <copyright file="CloneTransformAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Clone
{
    using BovineLabs.Core.Clone;
    using Unity.Entities;
    using UnityEngine;

    public class CloneTransformAuthoring : MonoBehaviour
    {
        public GameObject? Target;

        private class CloneTransformBaker : Baker<CloneTransformAuthoring>
        {
            public override void Bake(CloneTransformAuthoring authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.Dynamic);
                this.AddComponent(entity, new CloneTransform { Value = this.GetEntity(authoring.Target, TransformUsageFlags.Dynamic) });
            }
        }
    }
}
