// <copyright file="mathex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "SA1300", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "lower case causes issues")]
    public static class mathex
    {
        public const float Radians90 = math.PI / 2f;
        public const float Radians180 = math.PI;
        public const float Radians270 = (math.PI * 3f) / 2f;
        public const float Radians360 = math.PI * 2f;

        /// <summary>
        /// Returns the modulus of two numbers unlike % which returns the remainder.
        /// For positive values this is exactly the same as % just slower.
        /// </summary>
        /// <param name="x"> The dividend. </param>
        /// <param name="m"> The divisor. </param>
        /// <returns> The modulus. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int mod(int x, int m)
        {
            return ((x % m) + m) % m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool isodd(int x)
        {
            return (x % 2) != 0;
        }

        /// <summary> Calculates the maximum value. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The maximum value. float.MinValue if 0 length array is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float max(NativeArray<float> values)
        {
            return max((float*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the maximum value. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The maximum value. float.MinValue if 0 length array is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float max(float* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var maxValue4 = new float4(float.MinValue);
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((float4*)values)[iValue];
                maxValue4 = math.max(maxValue4, value4);
            }

            var maxValue = math.cmax(maxValue4);
            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                maxValue = math.max(maxValue, values[iValue]);
            }

            return maxValue;
        }

        /// <summary> Calculates the maximum value. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The maximum value. int.MinValue if 0 length array is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int max(NativeArray<int> values)
        {
            return max((int*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the maximum value. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The maximum value. int.MinValue if length 0 is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int max(int* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var maxValue4 = new int4(int.MinValue);
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((int4*)values)[iValue];
                maxValue4 = math.max(maxValue4, value4);
            }

            var maxValue = math.cmax(maxValue4);
            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                maxValue = math.max(maxValue, values[iValue]);
            }

            return maxValue;
        }

        /// <summary> Calculates the minimum value. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The maximum value. float.MaxValue if 0 length array is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float min(NativeArray<float> values)
        {
            return min((float*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the minimum value. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The maximum value. float.MaxValue if length 0 is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float min(float* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var minValue4 = new float4(float.MaxValue);
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((float4*)values)[iValue];
                minValue4 = math.min(minValue4, value4);
            }

            var minValue = math.cmin(minValue4);
            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                minValue = math.min(minValue, values[iValue]);
            }

            return minValue;
        }

        /// <summary> Calculates the minimum value. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The maximum value. int.MaxValue if 0 length array is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int min(NativeArray<int> values)
        {
            return min((int*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the minimum value. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The maximum value. int.MaxValue if length 0 is passed. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int min(int* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var minValue4 = new int4(int.MaxValue);
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((int4*)values)[iValue];
                minValue4 = math.min(minValue4, value4);
            }

            var minValue = math.cmin(minValue4);
            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                minValue = math.min(minValue, values[iValue]);
            }

            return minValue;
        }

        /// <summary> Calculates the sum of values. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The sum of values. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float sum(NativeArray<float> values)
        {
            return sum((float*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the sum of values. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The sum of values. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe float sum(float* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var sumValue = 0f;
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((float4*)values)[iValue];
                sumValue += math.csum(value4);
            }

            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                sumValue += values[iValue];
            }

            return sumValue;
        }

        /// <summary> Calculates the sum of values. </summary>
        /// <param name="values"> The data. </param>
        /// <returns> The sum of values. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int sum(NativeArray<int> values)
        {
            return sum((int*)values.GetUnsafeReadOnlyPtr(), values.Length);
        }

        /// <summary> Calculates the sum of values. </summary>
        /// <param name="values"> The data. </param>
        /// <param name="length"> The length of the data. </param>
        /// <returns> The sum of values. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int sum(int* values, [AssumeRange(0, int.MaxValue)] int length)
        {
            var sumValue = 0;
            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                var value4 = ((int4*)values)[iValue];
                sumValue += math.csum(value4);
            }

            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                sumValue += values[iValue];
            }

            return sumValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void add(NativeArray<int> output, NativeArray<int> input, int value)
        {
            Check.Assume(output.Length == input.Length);
            add((int*)output.GetUnsafePtr(), (int*)input.GetUnsafeReadOnlyPtr(), input.Length, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void add([NoAlias] int* dst, [ReadOnly] int* src, [AssumeRange(0, int.MaxValue)] int length, int value)
        {
            var dst4 = (int4*)dst;
            var src4 = (int4*)src;

            var numSamples4 = length >> 2;
            for (var iValue = 0; iValue < numSamples4; iValue++)
            {
                dst4[iValue] = src4[iValue] + value;
            }

            for (var iValue = numSamples4 << 2; iValue < length; iValue++)
            {
                dst[iValue] = src[iValue] + value;
            }
        }

        /// <summary> Converts a quaternion to euler. </summary>
        /// <remarks> Taken from Unity.Physics. </remarks>
        /// <param name="q"> The quaternion. </param>
        /// <param name="order"> The winding order. </param>
        /// <returns> Euler angles in radians. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToEuler(quaternion q, math.RotationOrder order = math.RotationOrder.Default)
        {
            const float epsilon = 1e-6f;

            // prepare the data
            var qv = q.value;
            var d1 = qv * qv.wwww * new float4(2.0f); // xw, yw, zw, ww
            var d2 = qv * qv.yzxw * new float4(2.0f); // xy, yz, zx, ww
            var d3 = qv * qv;
            var euler = new float3(0.0f);

            const float CUTOFF = (1.0f - (2.0f * epsilon)) * (1.0f - (2.0f * epsilon));

            switch (order)
            {
                case math.RotationOrder.ZYX:
                {
                    var y1 = d2.z + d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.x + d1.z;
                        var x2 = (d3.x + d3.w) - d3.y - d3.z;
                        var z1 = -d2.y + d1.x;
                        var z2 = (d3.z + d3.w) - d3.y - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // zxz
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.ZXY:
                {
                    var y1 = d2.y - d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.x + d1.z;
                        var x2 = (d3.y + d3.w) - d3.x - d3.z;
                        var z1 = d2.z + d1.y;
                        var z2 = (d3.z + d3.w) - d3.x - d3.y;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // zxz
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.YXZ:
                {
                    var y1 = d2.y + d1.x;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.z + d1.y;
                        var x2 = (d3.z + d3.w) - d3.x - d3.y;
                        var z1 = -d2.x + d1.z;
                        var z2 = (d3.y + d3.w) - d3.z - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // yzy
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.YZX:
                {
                    var y1 = d2.x - d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.z + d1.y;
                        var x2 = (d3.x + d3.w) - d3.z - d3.y;
                        var z1 = d2.y + d1.x;
                        var z2 = (d3.y + d3.w) - d3.x - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // yxy
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.XZY:
                {
                    var y1 = d2.x + d1.z;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.y + d1.x;
                        var x2 = (d3.y + d3.w) - d3.z - d3.x;
                        var z1 = -d2.z + d1.y;
                        var z2 = (d3.x + d3.w) - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // xyx
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), 0.0f);
                    }

                    break;
                }

                case math.RotationOrder.XYZ:
                {
                    var y1 = d2.z - d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = d2.y + d1.x;
                        var x2 = (d3.z + d3.w) - d3.y - d3.x;
                        var z1 = d2.x + d1.z;
                        var z2 = (d3.x + d3.w) - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else
                    {
                        // xzx
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                        var x1 = 2.0f * ((abcd.x * abcd.w) + (abcd.y * abcd.z)); // 2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }
            }

            return eulerReorderBack(euler, order);
        }

        // Radians
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);
            var result = SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = math.max(0.0001F, smoothTime);
            var omega = 2F / smoothTime;

            var x = omega * deltaTime;
            var exp = 1F / (1F + x + (0.48F * x * x) + (0.235F * x * x * x));
            var change = current - target;
            var originalTo = target;

            // Clamp maximum speed
            var maxChange = maxSpeed * smoothTime;
            change = math.clamp(change, -maxChange, maxChange);
            target = current - change;

            var temp = (currentVelocity + (omega * change)) * deltaTime;
            currentVelocity = (currentVelocity - (omega * temp)) * exp;
            var result = target + ((change + temp) * exp);

            // Prevent overshooting
            if (originalTo - current > 0.0F == result > originalTo)
            {
                result = originalTo;
                currentVelocity = (result - originalTo) / deltaTime;
            }

            return result;
        }

        /// <summary>
        /// Radians
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DeltaAngle(float current, float target)
        {
            var delta = Repeat(target - current, Radians360);
            return delta > Radians180 ? delta - Radians360 : delta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpAngle(float a, float b, float t)
        {
            var num = Repeat(b - a, Radians360);
            if (num > Radians180)
            {
                num -= Radians360;
            }

            return a + (num * math.clamp(t, 0, 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Repeat(float t, float length)
        {
            return math.clamp(t - (math.floor(t / length) * length), 0.0f, length);
        }

        // https://answers.unity.com/questions/47115/vector3-rotate-around.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotateAround(float3 point, float3 pivot, quaternion angle)
        {
            // Center the point around the origin
            var finalPos = point - pivot;

            // Rotate the point.
            finalPos = math.mul(angle, finalPos);

            // Move the point back to its original offset.
            finalPos += pivot;

            return finalPos;
        }

        /// <summary> Rotates a vector by angle in radians. </summary>
        /// <remarks>From https://matthew-brett.github.io/teaching/rotation_2d.html.</remarks>
        /// <param name="direction"> The original vector to rotate. </param>
        /// <param name="angle"> The angle to rotate by in radians. </param>
        /// <returns> The rotated vector. </returns>
        public static float2 Rotate(float2 direction, float angle)
        {
            var cos = math.cos(angle);
            var sin = math.sin(angle);
            return new float2((direction.x * cos) - (direction.y * sin), (direction.x * sin) + (direction.y * cos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreApproximatelyEqual(float2 f1, float2 f2, float delta = 0.01f)
        {
            return (math.abs(f1.x - f2.x) < delta) && (math.abs(f1.y - f2.y) < delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreApproximatelyEqual(float3 f1, float3 f2, float delta = 0.01f)
        {
            return (math.abs(f1.x - f2.x) < delta) && (math.abs(f1.y - f2.y) < delta) && (math.abs(f1.z - f2.z) < delta);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreApproximatelyEqual(float4 f1, float4 f2, float delta = 0.01f)
        {
            return math.all(math.abs(f1 - f2) < delta);
        }

        // fisher-yates-shuffle
        public static void Shuffle<T>(this NativeArray<T> array, ref Random random)
            where T : struct
        {
            for (var n = array.Length - 1; n > 0; n--)
            {
                var r = random.NextInt(n + 1);
                (array[r], array[n]) = (array[n], array[r]);
            }
        }

        /// <summary>
        /// Returns the 2D vector perpendicular to this 2D vector.
        /// The result is always rotated 90-degrees in a counter-clockwise direction for a 2D coordinate system where the positive Y axis goes up.
        /// </summary>
        /// <remarks> This is a copy of Vector2.Perpendicular. </remarks>
        /// <param name="inDirection"> The input direction. </param>
        /// <returns> The perpendicular direction. </returns>
        public static float2 Perpendicular(float2 inDirection)
        {
            return new float2(-inDirection.y, inDirection.x);
        }

        public static float NormalizeWithLength(float3 v, out float3 n)
        {
            var lengthSq = math.lengthsq(v);
            var invLength = math.rsqrt(lengthSq);
            n = v * invLength;
            return lengthSq * invLength;
        }

        /// <summary>  Return two normals perpendicular to the input vector. </summary>
        public static void CalculatePerpendicularNormalized(float3 v, out float3 p, out float3 q)
        {
            var vSquared = v * v;
            var lengthsSquared = vSquared + vSquared.xxx; // y = ||j x v||^2, z = ||k x v||^2
            var invLengths = math.rsqrt(lengthsSquared);

            // select first direction, j x v or k x v, whichever has greater magnitude
            var dir0 = new float3(-v.y, v.x, 0.0f);
            var dir1 = new float3(-v.z, 0.0f, v.x);
            var cmp = lengthsSquared.y > lengthsSquared.z;
            var dir = math.select(dir1, dir0, cmp);

            // normalize and get the other direction
            var invLength = math.select(invLengths.z, invLengths.y, cmp);
            p = dir * invLength;
            var cross = math.cross(v, dir);
            q = cross * invLength;
        }

        private static float3 eulerReorderBack(float3 euler, math.RotationOrder order)
        {
            switch (order)
            {
                case math.RotationOrder.XZY:
                    return euler.xzy;
                case math.RotationOrder.YZX:
                    return euler.zxy;
                case math.RotationOrder.YXZ:
                    return euler.yxz;
                case math.RotationOrder.ZXY:
                    return euler.yzx;
                case math.RotationOrder.ZYX:
                    return euler.zyx;
                case math.RotationOrder.XYZ:
                default:
                    return euler;
            }
        }
    }
}
