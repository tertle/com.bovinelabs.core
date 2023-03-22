// <copyright file="CopyTransformFromGameObjectAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_COPY_TRANSFORM
namespace BovineLabs.Core.Authoring.Hybrid
{
    using BovineLabs.Core.Hybrid;
    using Unity.Entities;
    using UnityEngine;

    public class CopyTransformFromGameObjectAuthoring : MonoBehaviour
    {
    }

    public class CopyTransformFromGameObjectBaker : Baker<CopyTransformFromGameObjectAuthoring>
    {
        public override void Bake(CopyTransformFromGameObjectAuthoring authoring)
        {
            this.AddComponent<CopyTransformFromGameObject>();
        }
    }
}
#endif
