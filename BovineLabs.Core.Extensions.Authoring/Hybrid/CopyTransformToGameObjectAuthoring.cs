// <copyright file="CopyTransformToGameObjectAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Authoring.Hybrid
{
    using BovineLabs.Core.Hybrid;
    using Unity.Entities;
    using UnityEngine;

    public class CopyTransformToGameObjectAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CopyTransformToGameObjectAuthoring>
        {
            public override void Bake(CopyTransformToGameObjectAuthoring authoring)
            {
                this.AddComponent<CopyTransformToGameObject>(this.GetEntity(TransformUsageFlags.Dynamic));
            }
        }
    }
}
#endif
