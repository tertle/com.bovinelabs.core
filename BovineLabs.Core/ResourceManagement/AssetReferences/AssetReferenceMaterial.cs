// <copyright file="AssetReferenceMaterial.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ResourceManagement
{
    using System;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    /// <summary> AssetReference for <see cref="Material"/>. </summary>
    [Serializable]
    public class AssetReferenceMaterial : AssetReferenceT<Material>
    {
        /// <summary> Initializes a new instance of the <see cref="AssetReferenceMaterial"/> class. </summary>
        /// <param name="guid"> The guid for the <see cref="Material"/>. </param>
        public AssetReferenceMaterial(string guid)
            : base(guid)
        {
        }
    }
}