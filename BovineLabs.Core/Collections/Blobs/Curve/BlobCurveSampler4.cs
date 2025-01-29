// <copyright file="BlobCurveSampler4.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler4 : IBlobCurveSampler<float4>
    {
        public BlobAssetReference<BlobCurve4> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler4(BlobAssetReference<BlobCurve4> curve)
        {
            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time)
        {
            return this.Curve.Value.Evaluate(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateWithoutCache(in float time)
        {
            return this.Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
