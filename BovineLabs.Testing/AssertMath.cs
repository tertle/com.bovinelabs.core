// <copyright file="AssertMath.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Testing
{
    using Unity.Mathematics;
    using UnityEngine.Assertions;

    /// <summary>
    /// The AssertExtensions.
    /// </summary>
    public static class AssertMath
    {
        /// <summary>
        /// Compares two quaternions to check if they are equal within delta range.
        /// </summary>
        /// <param name="expected"> The expected result. </param>
        /// <param name="result"> The actual result. </param>
        /// <param name="delta"> Delta. </param>
        public static void AreApproximatelyEqual(quaternion expected, quaternion result, float delta)
        {
            Assert.AreApproximatelyEqual(expected.value.x, result.value.x, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.y, result.value.y, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.z, result.value.z, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.w, result.value.w, delta, $"Expected: {result} == {expected}, +-{delta}");
        }

        /// <summary>
        /// Compares two float3 to check if they are equal within delta range.
        /// </summary>
        /// <param name="expected"> The expected result. </param>
        /// <param name="result"> The actual result. </param>
        /// <param name="delta"> Delta. </param>
        public static void AreApproximatelyEqual(float3 expected, float3 result, float delta)
        {
            Assert.AreApproximatelyEqual(expected.x, result.x, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.y, result.y, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.z, result.z, delta, $"Expected: {result} == {expected}, +-{delta}");
        }
    }
}
