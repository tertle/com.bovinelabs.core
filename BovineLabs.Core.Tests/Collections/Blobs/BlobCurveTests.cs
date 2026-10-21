namespace BovineLabs.Core.Tests.Collections.Blobs
{
    using System;
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;
    using UnityEngine;

    public class BlobCurveTests
    {
        [Test]
        public void CreateFromAnimationCurvesPreservesSamples()
        {
            var x = AnimationCurve.Linear(0f, 1f, 2f, 5f);
            var y = AnimationCurve.Linear(0f, -2f, 2f, 4f);
            var z = AnimationCurve.Linear(0f, 8f, 2f, 2f);
            var w = AnimationCurve.Linear(0f, -4f, 2f, -2f);
            const float time = 0.75f;

            using var curve = BlobCurve.Create(x, Allocator.Temp);
            using var curve2 = BlobCurve2.Create(x, y, Allocator.Temp);
            using var curve3 = BlobCurve3.Create(x, y, z, Allocator.Temp);
            using var curve4 = BlobCurve4.Create(x, y, z, w, Allocator.Temp);

            Assert.AreEqual(x.Evaluate(time), curve.Value.Evaluate(time), 0.0001f);
            AssertFloat2(new float2(x.Evaluate(time), y.Evaluate(time)), curve2.Value.Evaluate(time));
            AssertFloat3(new float3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time)), curve3.Value.Evaluate(time));
            AssertFloat4(
                new float4(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time), w.Evaluate(time)), curve4.Value.Evaluate(time));
        }


        private static void AssertFloat2(float2 expected, float2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
        }

        private static void AssertFloat3(float3 expected, float3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
        }

        private static void AssertFloat4(float4 expected, float4 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.AreEqual(expected.z, actual.z, 0.0001f);
            Assert.AreEqual(expected.w, actual.w, 0.0001f);
        }
    }
}
