namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using UnityEngine;

    public readonly struct BlobCurveSegment
    {
        private readonly float4 factors;

        public BlobCurveSegment(float4 factors)
        {
            this.factors = factors;
        }

        public BlobCurveSegment(Keyframe k0, Keyframe k1)
        {
            this.factors = BlobShared.UnityFactor(k0.value, k0.outTangent, k1.inTangent, k1.value, k1.time - k0.time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Sample(in float4 timeSerial)
        {
            return math.dot(this.factors, timeSerial);
        }
    }
}
