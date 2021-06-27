// <copyright file="AssetReferenceVisualTreeAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using System;
    using UnityEngine.AddressableAssets;
    using UnityEngine.UIElements;

    /// <summary> AssetReference for <see cref="VisualTreeAsset"/>. </summary>
    [Serializable]
    public class AssetReferenceVisualTreeAsset : AssetReferenceT<VisualTreeAsset>
    {
        /// <summary> Initializes a new instance of the <see cref="AssetReferenceVisualTreeAsset"/> class. </summary>
        /// <param name="guid"> The guid for the <see cref="VisualTreeAsset"/>. </param>
        public AssetReferenceVisualTreeAsset(string guid)
            : base(guid)
        {
        }
    }
}