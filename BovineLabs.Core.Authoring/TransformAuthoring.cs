// <copyright file="TransformAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using UnityEngine;

    public class TransformAuthoring : MonoBehaviour
    {
        public TransformUsageFlags TransformUsageFlags = TransformUsageFlags.Dynamic;
    }

    public class TransformBaker : Baker<TransformAuthoring>
    {
        public override void Bake(TransformAuthoring authoring)
        {
            this.GetEntity(authoring.TransformUsageFlags);
        }
    }
}
