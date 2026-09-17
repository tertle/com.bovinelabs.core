namespace BovineLabs.Testing
{
    using Unity.Mathematics;
    using UnityEngine.Assertions;

    public static class AssertMath
    {
        public static void AreApproximatelyEqual(quaternion expected, quaternion result, float delta)
        {
            Assert.AreApproximatelyEqual(expected.value.x, result.value.x, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.y, result.value.y, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.z, result.value.z, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.value.w, result.value.w, delta, $"Expected: {result} == {expected}, +-{delta}");
        }

        public static void AreApproximatelyEqual(float3 expected, float3 result, float delta)
        {
            Assert.AreApproximatelyEqual(expected.x, result.x, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.y, result.y, delta, $"Expected: {result} == {expected}, +-{delta}");
            Assert.AreApproximatelyEqual(expected.z, result.z, delta, $"Expected: {result} == {expected}, +-{delta}");
        }
    }
}
