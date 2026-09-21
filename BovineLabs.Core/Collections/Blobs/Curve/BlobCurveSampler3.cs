namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;
    using Unity.Mathematics;

    public struct BlobCurveSampler3 : IBlobCurveSampler<float3>
    {
        public BlobAssetReference<BlobCurve3> Curve;
        private BlobCurveCache _cache;

        public BlobCurveSampler3(BlobAssetReference<BlobCurve3> curve)
        {
            Curve = curve;
            _cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Evaluate(in float time)
        {
            return Curve.Value.Evaluate(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateIgnoreWrapMode(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time, ref _cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateWithoutCache(in float time)
        {
            return Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
