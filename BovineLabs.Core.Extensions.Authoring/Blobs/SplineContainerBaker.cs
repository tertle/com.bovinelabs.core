// <copyright file="SplineContainerBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_SPLINES && BL_BAKE_SPLINE
namespace BovineLabs.Core.Authoring.Blobs
{
    using BovineLabs.Core.Blobs;
    using BovineLabs.Core.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine.Splines;

    public class SplineContainerBaker : Baker<SplineContainer>
    {
        public override void Bake(SplineContainer authoring)
        {
            var entity = this.GetEntity(TransformUsageFlags.None);
            var blob = BlobSpline.Create<Spline>(authoring.Splines, float4x4.identity);

            this.AddBlobAsset(ref blob, out _);
            this.AddComponent(entity, new Splines { Value = blob });
        }
    }
}
#endif