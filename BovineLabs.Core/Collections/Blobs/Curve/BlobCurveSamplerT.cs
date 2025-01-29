// <copyright file="BlobCurveSamplerT.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public struct BlobCurveSampler<T> : IBlobCurveSampler<T>
        where T : unmanaged
    {
        public readonly BlobAssetReference<BlobCurve> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler(BlobAssetReference<BlobCurve> curve)
        {
            Check.Assume(UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<float>());

            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Evaluate(in float time)
        {
            var r = this.Curve.Value.Evaluate(time, ref this.cache);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapMode(in float time)
        {
            var r = this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateWithoutCache(in float time)
        {
            var r = this.Curve.Value.Evaluate(time);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            var r = this.Curve.Value.EvaluateIgnoreWrapMode(time);
            return UnsafeUtility.As<float, T>(ref r);
        }
    }
}
