// <copyright file="CopyTransformToGameObjectAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_COPY_TRANSFORM
namespace BovineLabs.Core.Authoring.Hybrid
{
    using BovineLabs.Core.Hybrid;
    using Unity.Entities;
    using UnityEngine;

    public class CopyTransformToGameObjectAuthoring : MonoBehaviour
    {
    }

    public class CopyTransformToGameObjectBaker : Baker<CopyTransformToGameObjectAuthoring>
    {
        public override void Bake(CopyTransformToGameObjectAuthoring authoring)
        {
            this.AddComponent<CopyTransformToGameObject>();
        }
    }
}
#endif
