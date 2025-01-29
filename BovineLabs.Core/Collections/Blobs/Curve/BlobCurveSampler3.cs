// <copyright file="BlobCurveSampler3.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler3 : IBlobCurveSampler<float3>
    {
        public BlobAssetReference<BlobCurve3> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler3(BlobAssetReference<BlobCurve3> curve)
        {
            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Evaluate(in float time)
        {
            return this.Curve.Value.Evaluate(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateIgnoreWrapMode(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateWithoutCache(in float time)
        {
            return this.Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
