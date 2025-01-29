// <copyright file="BlobCurveSegment2.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Assertions;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary>
    /// 2d AnimationCurveSegment using Cubic Bezier spline
    /// </summary>
    public struct BlobCurveSegment2
    {
        public float4x2 Factor;

        public float2 Sample(float4 timeSerial)
        {
            return math.mul(timeSerial, this.Factor);
        }

        /// <summary>
        /// Create from scratch
        /// </summary>
        public BlobCurveSegment2(float4 factorX, float4 factorY)
        {
            this.Factor = new float4x2(factorX, factorY);
        }

        /// <summary> Convert From Keyframe Pair </summary>
        public BlobCurveSegment2(Keyframe k0x, Keyframe k0y, Keyframe k1x, Keyframe k1y)
        {
            Assert.IsTrue(Mathf.Approximately(k0x.time, k0y.time) && Mathf.Approximately(k1x.time, k1y.time), "Time not sync");
            var duration = k1x.time - k0x.time;
            this.Factor = new float4x2(BlobShared.UnityFactor(k0x.value, k0x.outTangent, k1x.inTangent, k1x.value, duration),
                BlobShared.UnityFactor(k0y.value, k0y.outTangent, k1y.inTangent, k1y.value, duration));
        }

        /// <summary>
        /// Convert From UnityEngine.AnimationCurve parameter
        /// </summary>
        public static BlobCurveSegment2 Unity2(float2 value0, float2 tangent0, float2 tangent1, float2 value1, float duration)
        {
            return new BlobCurveSegment2(BlobShared.UnityFactor(value0.x, tangent0.x, tangent0.x, value1.x, duration),
                BlobShared.UnityFactor(value0.y, tangent0.y, tangent0.y, value1.y, duration));
        }

        /// <summary>
        /// Convert From Cubic Hermite spline parameter
        /// </summary>
        public static BlobCurveSegment2 Hermite2(float2 value0, float2 m0, float2 m1, float2 value1)
        {
            return new BlobCurveSegment2(BlobShared.HermiteFactor(value0.x, m0.x, m1.x, value1.x), BlobShared.HermiteFactor(value0.y, m0.y, m1.y, value1.y));
        }

        /// <summary>
        /// Convert From Cubic Bezier spline parameter
        /// </summary>
        public static BlobCurveSegment2 Bezier2(float2 value0, float2 cv0, float2 cv1, float2 value1)
        {
            return new BlobCurveSegment2(BlobShared.BezierFactor(value0.x, cv0.x, cv1.x, value1.x), BlobShared.BezierFactor(value0.y, cv0.y, cv1.y, value1.y));
        }

        /// <summary>
        /// Convert Linear Curve
        /// </summary>
        public static BlobCurveSegment2 Linear2(float2 value0, float2 value1)
        {
            return new BlobCurveSegment2(BlobShared.LinearFactor(value0.x, value1.x), BlobShared.LinearFactor(value0.y, value1.y));
        }
    }
}
