namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler4<T> : IBlobCurveSampler<T>
        where T : unmanaged
    {
        public readonly BlobAssetReference<BlobCurve4> Curve;
        private BlobCurveCache _cache;

        public BlobCurveSampler4(BlobAssetReference<BlobCurve4> curve)
        {
            Check.Assume(UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<float4>());

            Curve = curve;
            _cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Evaluate(in float time)
        {
            var r = Curve.Value.Evaluate(time, ref _cache);
            return UnsafeUtility.As<float4, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapMode(in float time)
        {
            var r = Curve.Value.EvaluateIgnoreWrapMode(time, ref _cache);
            return UnsafeUtility.As<float4, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateWithoutCache(in float time)
        {
            var r = Curve.Value.Evaluate(time);
            return UnsafeUtility.As<float4, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            var r = Curve.Value.EvaluateIgnoreWrapMode(time);
            return UnsafeUtility.As<float4, T>(ref r);
        }
    }
}
