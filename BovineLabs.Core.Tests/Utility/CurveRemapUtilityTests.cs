// <copyright file="CurveRemapUtilityTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using UnityEngine;

    public class CurveRemapUtilityTests
    {
        [Test]
        public void TryRemapToClipLengthRemapsTimesAndTangents()
        {
            var source = new AnimationCurve(
                new Keyframe(2f, 3f, float.PositiveInfinity, 4f, 0.25f, 0.5f),
                new Keyframe(4f, 7f, 8f, float.NegativeInfinity, 0.75f, 0.2f))
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.Clamp,
            };

            Assert.IsTrue(CurveRemapUtility.TryRemapToClipLength(source, 10f, 4f, out var result));

            Assert.AreEqual(2, result.length);
            Assert.AreEqual(source.preWrapMode, result.preWrapMode);
            Assert.AreEqual(source.postWrapMode, result.postWrapMode);
            AssertKey(result[0], 10f, 3f, float.PositiveInfinity, 2f, 0.25f, 0.5f);
            AssertKey(result[1], 14f, 7f, 4f, float.NegativeInfinity, 0.75f, 0.2f);
        }

        [Test]
        public void TryRemapToClipLengthMovesSingleKeyWithoutChangingTangents()
        {
            var source = new AnimationCurve(new Keyframe(2f, 3f, 4f, 5f));

            Assert.IsTrue(CurveRemapUtility.TryRemapToClipLength(source, 10f, 4f, out var result));

            Assert.AreEqual(1, result.length);
            AssertKey(result[0], 10f, 3f, 4f, 5f, 0f, 0f);
        }

        private static void AssertKey(
            Keyframe key, float time, float value, float inTangent, float outTangent, float inWeight, float outWeight)
        {
            AssertFloat(time, key.time);
            AssertFloat(value, key.value);
            AssertFloat(inTangent, key.inTangent);
            AssertFloat(outTangent, key.outTangent);
            AssertFloat(inWeight, key.inWeight);
            AssertFloat(outWeight, key.outWeight);
        }

        private static void AssertFloat(float expected, float actual)
        {
            if (float.IsInfinity(expected))
            {
                Assert.AreEqual(expected, actual);
                return;
            }

            Assert.AreEqual(expected, actual, 0.0001f);
        }
    }
}
