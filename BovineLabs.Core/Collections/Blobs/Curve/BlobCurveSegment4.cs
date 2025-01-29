// <copyright file="BlobCurveSegment4.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Assertions;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// AnimationCurveSegment using Cubic Bezier spline
    /// </summary>
    public struct BlobCurveSegment4
    {
        public float4x4 Factors;

        public float4 Sample(float4 timeSerial)
        {
            return math.mul(timeSerial, this.Factors);
        }

        /// <summary> Create from scratch. </summary>
        public BlobCurveSegment4(float4 factorX, float4 factorY, float4 factorZ, float4 factorW)
        {
            this.Factors = new float4x4(factorX, factorY, factorZ, factorW);
        }

        /// <summary> Convert From Keyframe Pair </summary>
        public BlobCurveSegment4(Keyframe k0x, Keyframe k0y, Keyframe k0z, Keyframe k0w, Keyframe k1x, Keyframe k1y, Keyframe k1z, Keyframe k1w)
        {
            Assert.IsTrue(
                Mathf.Approximately(k0x.time, k0y.time) &&
                Mathf.Approximately(k1x.time, k1y.time) &&
                Mathf.Approximately(k0x.time, k0z.time) &&
                Mathf.Approximately(k1x.time, k1z.time) &&
                Mathf.Approximately(k0x.time, k0w.time) &&
                Mathf.Approximately(k1x.time, k1w.time), "Time not sync");

            var duration = k1x.time - k0x.time;
            this.Factors = new float4x4(BlobShared.UnityFactor(k0x.value, k0x.outTangent, k1x.inTangent, k1x.value, duration),
                BlobShared.UnityFactor(k0y.value, k0y.outTangent, k1y.inTangent, k1y.value, duration),
                BlobShared.UnityFactor(k0z.value, k0z.outTangent, k1z.inTangent, k1z.value, duration),
                BlobShared.UnityFactor(k0w.value, k0w.outTangent, k1w.inTangent, k1w.value, duration));
        }

        /// <summary>
        /// Convert From UnityEngine.AnimationCurve parameter
        /// </summary>
        public static BlobCurveSegment4 Unity4(float4 value0, float4 tangent0, float4 tangent1, float4 value1, float duration)
        {
            return new BlobCurveSegment4(BlobShared.UnityFactor(value0.x, tangent0.x, tangent0.x, value1.x, duration),
                BlobShared.UnityFactor(value0.y, tangent0.y, tangent0.y, value1.y, duration),
                BlobShared.UnityFactor(value0.z, tangent0.z, tangent0.z, value1.z, duration),
                BlobShared.UnityFactor(value0.w, tangent0.w, tangent0.w, value1.w, duration));
        }

        /// <summary>
        /// Convert From Cubic Bezier spline parameter
        /// </summary>
        public static BlobCurveSegment4 Bezier4(float4 value0, float4 cv0, float4 cv1, float4 value1)
        {
            return new BlobCurveSegment4(BlobShared.BezierFactor(value0.x, cv0.x, cv1.x, value1.x), BlobShared.BezierFactor(value0.y, cv0.y, cv1.y, value1.y),
                BlobShared.BezierFactor(value0.z, cv0.z, cv1.z, value1.z), BlobShared.BezierFactor(value0.w, cv0.w, cv1.w, value1.w));
        }

        /// <summary>
        /// Convert Linear Curve
        /// </summary>
        public static BlobCurveSegment4 Linear4(float4 value0, float4 value1)
        {
            return new BlobCurveSegment4(BlobShared.LinearFactor(value0.x, value1.x), BlobShared.LinearFactor(value0.y, value1.y),
                BlobShared.LinearFactor(value0.z, value1.z), BlobShared.LinearFactor(value0.w, value1.w));
        }
    }
}
