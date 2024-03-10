namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Entities;

    public struct BlobCurveSampler : IBlobCurveSampler<float>
    {
        public readonly BlobAssetReference<BlobCurve> Curve;
        private BlobCurveCache cache;

        public BlobCurveSampler(BlobAssetReference<BlobCurve> curve)
        {
            this.Curve = curve;
            this.cache = BlobCurveCache.Empty;
        }

        public bool IsCreated => this.Curve.IsCreated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Evaluate(in float time)
        {
            return this.Curve.Value.Evaluate(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapMode(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time, ref this.cache);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateWithoutCache(in float time)
        {
            return this.Curve.Value.Evaluate(time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float EvaluateIgnoreWrapModeWithoutCache(in float time)
        {
            return this.Curve.Value.EvaluateIgnoreWrapMode(time);
        }
    }
}
