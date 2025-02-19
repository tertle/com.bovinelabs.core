// <copyright file="GlobalRandom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Entropy
{
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    using Random = Unity.Mathematics.Random;

    /// <summary> Globally accessible random values even from bursted jobs. </summary>
    public static class GlobalRandom
    {
        private static readonly SharedStatic<ThreadRandom> ThreadRandoms = SharedStatic<ThreadRandom>.GetOrCreate<RandomType>();

        /// <summary> Gets the random by ref for the executing thread. </summary>
        public static ref Random Thread => ref ThreadRandoms.Data.GetRandomRef();

        /// <inheritdoc cref="Random.NextBool()"/>
        public static bool NextBool()
        {
            return Thread.NextBool();
        }

        /// <inheritdoc cref="Random.NextBool2()"/>
        public static bool2 NextBool2()
        {
            return Thread.NextBool2();
        }

        /// <inheritdoc cref="Random.NextBool3()"/>
        public static bool3 NextBool3()
        {
            return Thread.NextBool3();
        }

        /// <inheritdoc cref="Random.NextBool4()"/>
        public static bool4 NextBool4()
        {
            return Thread.NextBool4();
        }

        /// <inheritdoc cref="Random.NextInt()"/>
        public static int NextInt()
        {
            return Thread.NextInt();
        }

        /// <inheritdoc cref="Random.NextInt2()"/>
        public static int2 NextInt2()
        {
            return Thread.NextInt2();
        }

        /// <inheritdoc cref="Random.NextInt3()"/>
        public static int3 NextInt3()
        {
            return Thread.NextInt3();
        }

        /// <inheritdoc cref="Random.NextInt4()"/>
        public static int4 NextInt4()
        {
            return Thread.NextInt4();
        }

        /// <inheritdoc cref="Random.NextInt(int)"/>
        public static int NextInt(int max)
        {
            return Thread.NextInt(max);
        }

        /// <inheritdoc cref="Random.NextInt2(int2)"/>
        public static int2 NextInt2(int2 max)
        {
            return Thread.NextInt2(max);
        }

        /// <inheritdoc cref="Random.NextInt3(int3)"/>
        public static int3 NextInt3(int3 max)
        {
            return Thread.NextInt3(max);
        }

        /// <inheritdoc cref="Random.NextInt4(int4)"/>
        public static int4 NextInt4(int4 max)
        {
            return Thread.NextInt4(max);
        }

        /// <inheritdoc cref="Random.NextInt(int, int)"/>
        public static int NextInt(int min, int max)
        {
            return Thread.NextInt(min, max);
        }

        /// <inheritdoc cref="Random.NextInt2(int2, int2)"/>
        public static int2 NextInt2(int2 min, int2 max)
        {
            return Thread.NextInt2(min, max);
        }

        /// <inheritdoc cref="Random.NextInt3(int3, int3)"/>
        public static int3 NextInt3(int3 min, int3 max)
        {
            return Thread.NextInt3(min, max);
        }

        /// <inheritdoc cref="Random.NextInt4(int4, int4)"/>
        public static int4 NextInt4(int4 min, int4 max)
        {
            return Thread.NextInt4(min, max);
        }

        /// <inheritdoc cref="Random.NextUInt()"/>
        public static uint NextUInt()
        {
            return Thread.NextUInt();
        }

        /// <inheritdoc cref="Random.NextUInt2()"/>
        public static uint2 NextUInt2()
        {
            return Thread.NextUInt2();
        }

        /// <inheritdoc cref="Random.NextUInt3()"/>
        public static uint3 NextUInt3()
        {
            return Thread.NextUInt3();
        }

        /// <inheritdoc cref="Random.NextUInt4()"/>
        public static uint4 NextUInt4()
        {
            return Thread.NextUInt4();
        }

        /// <inheritdoc cref="Random.NextUInt(uint)"/>
        public static uint NextUInt(uint max)
        {
            return Thread.NextUInt(max);
        }

        /// <inheritdoc cref="Random.NextUInt2(uint2)"/>
        public static uint2 NextUInt2(uint2 max)
        {
            return Thread.NextUInt2(max);
        }

        /// <inheritdoc cref="Random.NextUInt3(uint3)"/>
        public static uint3 NextUInt3(uint3 max)
        {
            return Thread.NextUInt3(max);
        }

        /// <inheritdoc cref="Random.NextUInt4(uint4)"/>
        public static uint4 NextUInt4(uint4 max)
        {
            return Thread.NextUInt4(max);
        }

        /// <inheritdoc cref="Random.NextUInt(uint, uint)"/>
        public static uint NextUInt(uint min, uint max)
        {
            return Thread.NextUInt(min, max);
        }

        /// <inheritdoc cref="Random.NextUInt2(uint2, uint2)"/>
        public static uint2 NextUInt2(uint2 min, uint2 max)
        {
            return Thread.NextUInt2(min, max);
        }

        /// <inheritdoc cref="Random.NextUInt3(uint3, uint3)"/>
        public static uint3 NextUInt3(uint3 min, uint3 max)
        {
            return Thread.NextUInt3(min, max);
        }

        /// <inheritdoc cref="Random.NextUInt4(uint4, uint4)"/>
        public static uint4 NextUInt4(uint4 min, uint4 max)
        {
            return Thread.NextUInt4(min, max);
        }

        /// <inheritdoc cref="Random.NextFloat()"/>
        public static float NextFloat()
        {
            return Thread.NextFloat();
        }

        /// <inheritdoc cref="Random.NextFloat2()"/>
        public static float2 NextFloat2()
        {
            return Thread.NextFloat2();
        }

        /// <inheritdoc cref="Random.NextFloat3()"/>
        public static float3 NextFloat3()
        {
            return Thread.NextFloat3();
        }

        /// <inheritdoc cref="Random.NextFloat4()"/>
        public static float4 NextFloat4()
        {
            return Thread.NextFloat4();
        }

        /// <inheritdoc cref="Random.NextFloat(float)"/>
        public static float NextFloat(float max)
        {
            return Thread.NextFloat(max);
        }

        /// <inheritdoc cref="Random.NextFloat2(float2)"/>
        public static float2 NextFloat2(float2 max)
        {
            return Thread.NextFloat2(max);
        }

        /// <inheritdoc cref="Random.NextFloat3(float3)"/>
        public static float3 NextFloat3(float3 max)
        {
            return Thread.NextFloat3(max);
        }

        /// <inheritdoc cref="Random.NextFloat4(float4)"/>
        public static float4 NextFloat4(float4 max)
        {
            return Thread.NextFloat4(max);
        }

        /// <inheritdoc cref="Random.NextFloat(float, float)"/>
        public static float NextFloat(float min, float max)
        {
            return Thread.NextFloat(min, max);
        }

        /// <inheritdoc cref="Random.NextFloat2(float2, float2)"/>
        public static float2 NextFloat2(float2 min, float2 max)
        {
            return Thread.NextFloat2(min, max);
        }

        /// <inheritdoc cref="Random.NextFloat3(float3, float3)"/>
        public static float3 NextFloat3(float3 min, float3 max)
        {
            return Thread.NextFloat3(min, max);
        }

        /// <inheritdoc cref="Random.NextFloat4(float4, float4)"/>
        public static float4 NextFloat4(float4 min, float4 max)
        {
            return Thread.NextFloat4(min, max);
        }

        /// <inheritdoc cref="Random.NextDouble()"/>
        public static double NextDouble()
        {
            return Thread.NextDouble();
        }

        /// <inheritdoc cref="Random.NextDouble2()"/>
        public static double2 NextDouble2()
        {
            return Thread.NextDouble2();
        }

        /// <inheritdoc cref="Random.NextDouble3()"/>
        public static double3 NextDouble3()
        {
            return Thread.NextDouble3();
        }

        /// <inheritdoc cref="Random.NextDouble4()"/>
        public static double4 NextDouble4()
        {
            return Thread.NextDouble4();
        }

        /// <inheritdoc cref="Random.NextDouble(double)"/>
        public static double NextDouble(double max)
        {
            return Thread.NextDouble(max);
        }

        /// <inheritdoc cref="Random.NextDouble2(double2)"/>
        public static double2 NextDouble2(double2 max)
        {
            return Thread.NextDouble2(max);
        }

        /// <inheritdoc cref="Random.NextDouble3(double3)"/>
        public static double3 NextDouble3(double3 max)
        {
            return Thread.NextDouble3(max);
        }

        /// <inheritdoc cref="Random.NextDouble4(double4)"/>
        public static double4 NextDouble4(double4 max)
        {
            return Thread.NextDouble4(max);
        }

        /// <inheritdoc cref="Random.NextDouble(double, double)"/>
        public static double NextDouble(double min, double max)
        {
            return Thread.NextDouble(min, max);
        }

        /// <inheritdoc cref="Random.NextDouble2(double2, double2)"/>
        public static double2 NextDouble2(double2 min, double2 max)
        {
            return Thread.NextDouble2(min, max);
        }

        /// <inheritdoc cref="Random.NextDouble3(double3, double3)"/>
        public static double3 NextDouble3(double3 min, double3 max)
        {
            return Thread.NextDouble3(min, max);
        }

        /// <inheritdoc cref="Random.NextDouble4(double4, double4)"/>
        public static double4 NextDouble4(double4 min, double4 max)
        {
            return Thread.NextDouble4(min, max);
        }

        /// <inheritdoc cref="Random.NextFloat2Direction()"/>
        public static float2 NextFloat2Direction()
        {
            return Thread.NextFloat2Direction();
        }

        /// <inheritdoc cref="Random.NextFloat3Direction()"/>
        public static float3 NextFloat3Direction()
        {
            return Thread.NextFloat3Direction();
        }

        /// <inheritdoc cref="Random.NextFloat2Direction()"/>
        public static double2 NextDouble2Direction()
        {
            return Thread.NextDouble2Direction();
        }

        /// <inheritdoc cref="Random.NextDouble3Direction()"/>
        public static double3 NextDouble3Direction()
        {
            return Thread.NextDouble3Direction();
        }

        /// <inheritdoc cref="Random.NextQuaternionRotation()"/>
        public static quaternion NextQuaternionRotation()
        {
            return Thread.NextQuaternionRotation();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (ThreadRandoms.Data.IsCreated)
            {
                return;
            }

            ThreadRandoms.Data = new ThreadRandom((uint)UnityEngine.Random.Range(0, int.MaxValue), Allocator.Domain);
        }

        private struct RandomType
        {
        }
    }
}
