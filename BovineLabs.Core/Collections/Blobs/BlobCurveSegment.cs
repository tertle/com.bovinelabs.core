// <copyright file="BlobCurveSegment.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary> AnimationCurveSegment using Cubic Bezier spline. </summary>
    public readonly struct BlobCurveSegment
    {
        // v0  m0  m1  v1
        private static readonly float4x4 HermiteMat = new(
            02, 01, 01, -2, //t^3
            -3, -2, -1, 03, //t^2
            00, 01, 00, 00, //t^1
            01, 00, 00, 00  //t^0
        );

        // p0  p1  p2  p3
        private static readonly float4x4 BezierMat = new(
            -1, 03, -3, 01, //t^3
            03, -6, 03, 00, //t^2
            -3, 03, 00, 00, //t^1
            01, 00, 00, 00  //t^0
        );

        private readonly float4 factors;

        /// <summary> Create from scratch </summary>
        public BlobCurveSegment(float4 factors)
        {
            this.factors = factors;
        }

        /// <summary> Convert From Keyframe Pair </summary>
        public BlobCurveSegment(Keyframe k0, Keyframe k1)
        {
            this.factors = UnityFactor(k0.value, k0.outTangent, k1.inTangent, k1.value, k1.time - k0.time);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Sample(in float4 timeSerial)
        {
            return math.dot(this.factors, timeSerial);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 UnityFactor(float v0, float t0, float t1, float v1, float duration)
        {
            return math.select(HermiteFactor(v0, t0 * duration, t1 * duration, v1), BezierFactor(v0, v0, v0, v0), math.isinf(t0) | math.isinf(t1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 HermiteFactor(float v0, float m0, float m1, float v1)
        {
            return math.mul(HermiteMat, new float4(v0, m0, m1, v1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 BezierFactor(float p0, float p1, float p2, float p3)
        {
            return math.mul(BezierMat, new float4(p0, p1, p2, p3));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4 LinearFactor(float p0, float p3)
        {
            var offset = (p3 - p0) / 3f;
            return BezierFactor(p0, p0 + offset, p3 - offset, p3);
        }
    }
}
