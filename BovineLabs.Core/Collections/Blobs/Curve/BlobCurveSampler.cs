namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;

    public struct BlobCurveSampler : IBlobCurveSampler<float>
    {
        public readonly BlobAssetReference<BlobCurve> Curve;
        private BlobCurveCache _cache;

        public BlobCurveSampler(BlobAssetReference<BlobCurve> curve)
        {
            Curve = curve;
            _cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(in float time)
        {
            return Curve.Value.Evaluate(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapMode(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateWithoutCache(in float time)
        {
            return Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
