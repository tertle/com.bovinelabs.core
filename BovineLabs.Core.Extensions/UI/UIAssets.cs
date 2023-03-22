// <copyright file="UIAssets.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using Unity.Entities;
    using UnityEngine.UIElements;

    public class UIAssets : IComponentData
    {
        public VisualTreeAsset[] Assets = Array.Empty<VisualTreeAsset>();
        public Guid StringLocalization;
    }
}
#endif
