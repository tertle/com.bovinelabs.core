namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler4 : IBlobCurveSampler<float4>
    {
        public BlobAssetReference<BlobCurve4> Curve;
        private BlobCurveCache _cache;

        public BlobCurveSampler4(BlobAssetReference<BlobCurve4> curve)
        {
            Curve = curve;
            _cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 Evaluate(in float time)
        {
            return Curve.Value.Evaluate(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapMode(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateWithoutCache(in float time)
        {
            return Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float4 EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
