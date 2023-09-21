// <copyright file="BlobCurveSampler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveCache
    {
        public static readonly BlobCurveCache Empty = new() { Index = int.MinValue, NeighborhoodTimes = float.NaN };
        public float2 NeighborhoodTimes;
        public int Index;
    }

    public struct BlobCurveSampler
    {
        private readonly BlobAssetReference<BlobCurve> curve;
        private BlobCurveCache cache;

        public BlobCurveSampler(BlobAssetReference<BlobCurve> curve)
        {
            this.curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(in float time)
        {
            return this.curve.Value.Evaluate(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapMode(in float time)
        {
            return this.curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateWithoutCache(in float time)
        {
            return this.curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return this.curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
