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
        private BlobCurveCache _cache;

        public BlobCurveSampler(BlobAssetReference<BlobCurve> curve)
        {
            Check.Assume(UnsafeUtility.SizeOf<T>() == UnsafeUtility.SizeOf<float>());

            Curve = curve;
            _cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Evaluate(in float time)
        {
            var r = Curve.Value.Evaluate(time, ref _cache);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapMode(in float time)
        {
            var r = Curve.Value.EvaluateIgnoreWrapMode(time, ref _cache);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateWithoutCache(in float time)
        {
            var r = Curve.Value.Evaluate(time);
            return UnsafeUtility.As<float, T>(ref r);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            var r = Curve.Value.EvaluateIgnoreWrapMode(time);
            return UnsafeUtility.As<float, T>(ref r);
        }
    }
}
