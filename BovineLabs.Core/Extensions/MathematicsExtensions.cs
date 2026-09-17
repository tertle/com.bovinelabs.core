namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;

    public static class MathematicsExtensions
    {
        public static AABB Encapsulate(this AABB aabb, AABB bounds)
        {
            var min = bounds.Min;
            var max = bounds.Max;

            aabb = new MinMaxAABB
            {
                Min = math.min(aabb.Min, min),
                Max = math.max(aabb.Max, min),
            };

            aabb = new MinMaxAABB
            {
                Min = math.min(aabb.Min, max),
                Max = math.max(aabb.Max, max),
            };

            return aabb;
        }

        public static AABB Expand(this AABB aabb, float expand)
        {
            aabb.Extents += expand * 0.5f;
            return aabb;
        }

        public static bool IsDefault(this AABB aabb)
        {
            return aabb.Center.Equals(float3.zero) && aabb.Extents.Equals(float3.zero);
        }

        public static float3 Right(this float4x4 value)
        {
            return new float3(value.c0.x, value.c0.y, value.c0.z);
        }

        public static float3 Up(this float4x4 value)
        {
            return new float3(value.c1.x, value.c1.y, value.c1.z);
        }

        public static float3 Forward(this float4x4 value)
        {
            return new float3(value.c2.x, value.c2.y, value.c2.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Right(this quaternion value)
        {
            return math.mul(value, math.right());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Up(this quaternion value)
        {
            return math.mul(value, math.up());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Forward(this quaternion value)
        {
            return math.mul(value, math.forward());
        }

        public static float3 Position(this float4x4 value)
        {
            return new float3(value.c3.x, value.c3.y, value.c3.z);
        }

        public static quaternion Rotation(this float4x4 value)
        {
            return new quaternion(value);
        }
    }
}
