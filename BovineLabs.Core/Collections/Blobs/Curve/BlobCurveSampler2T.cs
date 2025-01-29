// <copyright file="BlobCurveSampler2T.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler2<T> : IBlobCurveSampler<T>
        where T : unmanaged
    {
        public readonly BlobAssetReference<BlobCurve2> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler2(BlobAssetReference<BlobCurve2> curve)
        {
            Check.Assume(UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<float2>());

            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Evaluate(in float time)
        {
            var r = this.Curve.Value.Evaluate(time, ref this.cache);
            return UnsafeUtility.As<float2, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapMode(in float time)
        {
            var r = this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
            return UnsafeUtility.As<float2, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateWithoutCache(in float time)
        {
            var r = this.Curve.Value.Evaluate(time);
            return UnsafeUtility.As<float2, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            var r = this.Curve.Value.EvaluateIgnoreWrapMode(time);
            return UnsafeUtility.As<float2, T>(ref r);
        }
    }
}
