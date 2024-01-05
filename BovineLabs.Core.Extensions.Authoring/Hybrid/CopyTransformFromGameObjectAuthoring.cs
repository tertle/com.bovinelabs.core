// <copyright file="CopyTransformFromGameObjectAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Authoring.Hybrid
{
    using BovineLabs.Core.Hybrid;
    using Unity.Entities;
    using UnityEngine;

    public class CopyTransformFromGameObjectAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CopyTransformFromGameObjectAuthoring>
        {
            public override void Bake(CopyTransformFromGameObjectAuthoring authoring)
            {
                this.AddComponent<CopyTransformFromGameObject>(this.GetEntity(TransformUsageFlags.Dynamic));
            }
        }
    }
}
#endif
