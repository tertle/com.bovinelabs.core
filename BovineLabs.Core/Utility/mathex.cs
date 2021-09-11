// <copyright file="mathex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.CompilerServices;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "SA1300", Justification = "matching mathematics package")]
    public static class mathex
    {
        public const float Radians90 = math.PI / 2f;
        public const float Radians180 = math.PI;
        public const float Radians270 = math.PI * 3f / 2f;
        public const float Radians360 = math.PI * 2f;

        /// <summary>
        /// Returns the modulus of two numbers unlike % which returns the remainder.
        /// For positive values this is exactly the same as % just slower.
        /// </summary>
        /// <param name="x"> The left value. </param>
        /// <param name="m"> Thge </param>
        /// <returns> The modulus. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int mod(int x, int m)
        {
            return ((x % m) + m) % m;
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

            const float CUTOFF = (1.0f - 2.0f * epsilon) * (1.0f - 2.0f * epsilon);

            switch (order)
            {
                case math.RotationOrder.ZYX:
                {
                    var y1 = d2.z + d1.y;
                    if (y1 * y1 < CUTOFF)
                    {
                        var x1 = -d2.x + d1.z;
                        var x2 = d3.x + d3.w - d3.y - d3.z;
                        var z1 = -d2.y + d1.x;
                        var z2 = d3.z + d3.w - d3.y - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
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
                        var x2 = d3.y + d3.w - d3.x - d3.z;
                        var z1 = d2.z + d1.y;
                        var z2 = d3.z + d3.w - d3.x - d3.y;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else //zxz
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
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
                        var x2 = d3.z + d3.w - d3.x - d3.y;
                        var z1 = -d2.x + d1.z;
                        var z2 = d3.y + d3.w - d3.z - d3.x;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //yzy
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
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
                        var x2 = d3.x + d3.w - d3.z - d3.y;
                        var z1 = d2.y + d1.x;
                        var z2 = d3.y + d3.w - d3.x - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    }
                    else //yxy
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.y, d1.x);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
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
                        var x2 = d3.y + d3.w - d3.z - d3.x;
                        var z1 = -d2.z + d1.y;
                        var z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), math.asin(y1), math.atan2(z1, z2));
                    }
                    else //xyx
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.x, d1.z, d2.z, d1.y);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
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
                        var x2 = d3.z + d3.w - d3.y - d3.x;
                        var z1 = d2.x + d1.z;
                        var z2 = d3.x + d3.w - d3.y - d3.z;
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), math.atan2(z1, z2));
                    } else //xzx
                    {
                        y1 = math.clamp(y1, -1.0f, 1.0f);
                        var abcd = new float4(d2.z, d1.y, d2.x, d1.z);
                        var x1 = 2.0f * (abcd.x * abcd.w + abcd.y * abcd.z); //2(ad+bc)
                        var x2 = math.csum(abcd * abcd * new float4(-1.0f, 1.0f, -1.0f, 1.0f));
                        euler = new float3(math.atan2(x1, x2), -math.asin(y1), 0.0f);
                    }

                    break;
                }
            }

            return eulerReorderBack(euler, order);
        }

        static float3 eulerReorderBack(float3 euler, math.RotationOrder order)
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
