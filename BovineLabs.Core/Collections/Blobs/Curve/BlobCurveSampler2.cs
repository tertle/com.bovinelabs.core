// <copyright file="BlobCurveSampler2.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler2 : IBlobCurveSampler<float2>
    {
        public BlobAssetReference<BlobCurve2> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler2(BlobAssetReference<BlobCurve2> curve)
        {
            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 Evaluate(in float time)
        {
            return this.Curve.Value.Evaluate(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapMode(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateWithoutCache(in float time)
        {
            return this.Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
