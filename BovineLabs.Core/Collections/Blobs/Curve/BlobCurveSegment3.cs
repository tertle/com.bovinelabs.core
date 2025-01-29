// <copyright file="BlobCurveSegment3.cs" company="BovineLabs">
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
    public struct BlobCurveSegment3
    {
        // public BlobCurveSegment X;
        // public BlobCurveSegment Y;
        // public BlobCurveSegment Z;
        public float4x3 Factors;

        public float3 Sample(float4 timeSerial)
        {
            return math.mul(timeSerial, this.Factors);
        }

        /// <summary>
        /// Create from scratch
        /// </summary>
        public BlobCurveSegment3(float4 factorX, float4 factorY, float4 factorZ)
        {
            this.Factors = new float4x3(factorX, factorY, factorZ);
        }

        /// <summary> Convert From Keyframe Pair </summary>
        public BlobCurveSegment3(Keyframe k0x, Keyframe k0y, Keyframe k0z, Keyframe k1x, Keyframe k1y, Keyframe k1z)
        {
            Assert.IsTrue(
                Mathf.Approximately(k0x.time, k0y.time) &&
                Mathf.Approximately(k1x.time, k1y.time) &&
                Mathf.Approximately(k0x.time, k0z.time) &&
                Mathf.Approximately(k1x.time, k1z.time), "Time not sync");

            var duration = k1x.time - k0x.time;
            this.Factors = new float4x3(BlobShared.UnityFactor(k0x.value, k0x.outTangent, k1x.inTangent, k1x.value, duration),
                BlobShared.UnityFactor(k0y.value, k0y.outTangent, k1y.inTangent, k1y.value, duration),
                BlobShared.UnityFactor(k0z.value, k0z.outTangent, k1z.inTangent, k1z.value, duration));
        }

        /// <summary>
        /// Convert From UnityEngine.AnimationCurve parameter
        /// </summary>
        public static BlobCurveSegment3 Unity3(float3 value0, float3 tangent0, float3 tangent1, float3 value1, float duration)
        {
            return new BlobCurveSegment3(BlobShared.UnityFactor(value0.x, tangent0.x, tangent0.x, value1.x, duration),
                BlobShared.UnityFactor(value0.y, tangent0.y, tangent0.y, value1.y, duration),
                BlobShared.UnityFactor(value0.z, tangent0.z, tangent0.z, value1.z, duration));
        }

        /// <summary>
        /// Convert From Cubic Bezier spline parameter
        /// </summary>
        public static BlobCurveSegment3 Bezier3(float3 value0, float3 cv0, float3 cv1, float3 value1)
        {
            return new BlobCurveSegment3(BlobShared.BezierFactor(value0.x, cv0.x, cv1.x, value1.x), BlobShared.BezierFactor(value0.y, cv0.y, cv1.y, value1.y),
                BlobShared.BezierFactor(value0.z, cv0.z, cv1.z, value1.z));
        }

        /// <summary>
        /// Convert Linear Curve
        /// </summary>
        public static BlobCurveSegment3 Linear3(float3 value0, float3 value1)
        {
            return new BlobCurveSegment3(BlobShared.LinearFactor(value0.x, value1.x), BlobShared.LinearFactor(value0.y, value1.y),
                BlobShared.LinearFactor(value0.z, value1.z));
        }
    }
}
