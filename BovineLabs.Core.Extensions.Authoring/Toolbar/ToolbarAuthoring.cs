// <copyright file="ToolbarAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Authoring.Toolbar
{
    using System;
    using BovineLabs.Core.Toolbar;
    using Unity.Entities;
    using UnityEngine;

    public class ToolbarAuthoring : MonoBehaviour
    {
        public ToolbarAssets.KeyAssetPair[] Assets = Array.Empty<ToolbarAssets.KeyAssetPair>();
    }

#if UNITY_EDITOR || BL_DEBUG
    public class ToolbarBaker : Baker<ToolbarAuthoring>
    {
        public override void Bake(ToolbarAuthoring authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.None);
            this.AddComponentObject(entity, new ToolbarAssets { Assets = authoring.Assets });
        }
    }
#endif
}
#endif
