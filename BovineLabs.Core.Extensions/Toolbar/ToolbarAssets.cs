// <copyright file="ToolbarAssets.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && (UNITY_EDITOR || BL_DEBUG)
namespace BovineLabs.Core.Toolbar
{
    using System;
    using Unity.Entities;
    using UnityEngine.UIElements;

    public class ToolbarAssets : IComponentData
    {
        public KeyAssetPair[] Assets = Array.Empty<KeyAssetPair>();

        [Serializable]
        public struct KeyAssetPair
        {
            public string Name;
            public VisualTreeAsset Asset;
        }
    }
}
#endif
