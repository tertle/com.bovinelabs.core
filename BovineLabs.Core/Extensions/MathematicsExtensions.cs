// <copyright file="MathematicsExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Mathematics;

    /// <summary> Extensions for the mathematics package. </summary>
    public static class MathematicsExtensions
    {
        /// <summary> Converts a quaternion to euler. </summary>
        /// <param name="quaternion"> The quaternion. </param>
        /// <returns> Euler angles in radians. </returns>
        public static float3 ToEuler(this quaternion quaternion)
        {
            var q = quaternion.value;

            // roll (x-axis rotation)
            var sinRCosP = 2 * ((q.w * q.x) + (q.y * q.z));
            var cosRCosP = 1 - (2 * ((q.x * q.x) + (q.y * q.y)));
            var roll = math.atan2(sinRCosP, cosRCosP);

            // pitch (y-axis rotation)
            var sinP = 2 * ((q.w * q.y) - (q.z * q.x));
            var pitch = math.select(math.asin(sinP), (math.sign(sinP) * math.PI) / 2, math.abs(sinP) >= 1);

            // yaw (z-axis rotation)
            var sinYCosP = 2 * ((q.w * q.z) + (q.x * q.y));
            var cosYCosP = 1 - (2 * ((q.y * q.y) + (q.z * q.z)));
            var yaw = math.atan2(sinYCosP, cosYCosP);

            return new float3(roll, pitch, yaw);
        }

        /// <summary> Encapsulates two AABBs. </summary>
        /// <param name="aabb"> The base AABB. </param>
        /// <param name="bounds"> The second AABB. </param>
        /// <returns> The new AABB that encapsulates both AABBs. </returns>
        public static AABB Encapsulate(this AABB aabb, AABB bounds)
        {
            var min = bounds.Min;
            var max = bounds.Max;

            aabb = new MinMaxAABB { Min = math.min(aabb.Min, min), Max = math.max(aabb.Max, min) };
            aabb = new MinMaxAABB { Min = math.min(aabb.Min, max), Max = math.max(aabb.Max, max) };
            return aabb;
        }

        /// <summary> Expands the size of an AABB. </summary>
        /// <param name="aabb"> The AABB. </param>
        /// <param name="expand"> The expansion amount. </param>
        /// <returns> The new AABB. </returns>
        public static AABB Expand(this AABB aabb, float expand)
        {
            aabb.Extents += expand * 0.5f;
            return aabb;
        }

        /// <summary> Returns if a AABB is default. </summary>
        /// <param name="aabb"> The aabb. </param>
        /// <returns> True if default. </returns>
        public static bool IsDefault(this AABB aabb)
        {
            return aabb.Center.Equals(float3.zero) &&
                   aabb.Extents.Equals(float3.zero);
        }

        /// <summary> Get the right vector from a transformation matrix. </summary>
        /// <param name="value"> The transformation matrix. </param>
        /// <returns> The right vector. </returns>
        public static float3 Right(this float4x4 value)
        {
            return new float3(value.c0.x, value.c0.y, value.c0.z);
        }

        /// <summary> Get the up vector from a transformation matrix. </summary>
        /// <param name="value"> The transformation matrix. </param>
        /// <returns> The up vector. </returns>
        public static float3 Up(this float4x4 value)
        {
            return new float3(value.c1.x, value.c1.y, value.c1.z);
        }

        /// <summary> Get the forward vector from a transformation matrix. </summary>
        /// <param name="value"> The transformation matrix. </param>
        /// <returns> The forward vector. </returns>
        public static float3 Forward(this float4x4 value)
        {
            return new float3(value.c2.x, value.c2.y, value.c2.z);
        }

        /// <summary> Get the position from a transformation matrix. </summary>
        /// <param name="value"> The transformation matrix. </param>
        /// <returns> The position. </returns>
        public static float3 Position(this float4x4 value)
        {
            return new float3(value.c3.x, value.c3.y, value.c3.z);
        }

        /// <summary> Get the rotation from a transformation matrix. </summary>
        /// <param name="value"> The transformation matrix. </param>
        /// <returns> The rotation. </returns>
        public static quaternion Rotation(this float4x4 value)
        {
            return new quaternion(value);
        }
    }
}