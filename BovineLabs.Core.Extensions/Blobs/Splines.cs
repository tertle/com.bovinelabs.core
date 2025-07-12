// <copyright file="Splines.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES && BL_BAKE_SPLINE
namespace BovineLabs.Core.Blobs
{
    using BovineLabs.Core.Collections;
    using Unity.Entities;

    public struct Splines : IComponentData
    {
        public BlobAssetReference<BlobArray<BlobSpline>> Value;
    }
}
#endif