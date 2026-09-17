namespace BovineLabs.Core.Utility
{
    using BovineLabs.Core.Collections;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Scripting.LifecycleManagement;
    using Random = Unity.Mathematics.Random;

    public static partial class GlobalRandom
    {
        private static readonly SharedStatic<ThreadRandom> ThreadRandoms = SharedStatic<ThreadRandom>.GetOrCreate<RandomType>();

        public static ref Random Thread => ref ThreadRandoms.Data.GetRandomRef();

        public static bool NextBool()
        {
            return Thread.NextBool();
        }

        public static bool2 NextBool2()
        {
            return Thread.NextBool2();
        }

        public static bool3 NextBool3()
        {
            return Thread.NextBool3();
        }

        public static bool4 NextBool4()
        {
            return Thread.NextBool4();
        }

        public static int NextInt()
        {
            return Thread.NextInt();
        }

        public static int2 NextInt2()
        {
            return Thread.NextInt2();
        }

        public static int3 NextInt3()
        {
            return Thread.NextInt3();
        }

        public static int4 NextInt4()
        {
            return Thread.NextInt4();
        }

        public static int NextInt(int max)
        {
            return Thread.NextInt(max);
        }

        public static int2 NextInt2(int2 max)
        {
            return Thread.NextInt2(max);
        }

        public static int3 NextInt3(int3 max)
        {
            return Thread.NextInt3(max);
        }

        public static int4 NextInt4(int4 max)
        {
            return Thread.NextInt4(max);
        }

        public static int NextInt(int min, int max)
        {
            return Thread.NextInt(min, max);
        }

        public static int2 NextInt2(int2 min, int2 max)
        {
            return Thread.NextInt2(min, max);
        }

        public static int3 NextInt3(int3 min, int3 max)
        {
            return Thread.NextInt3(min, max);
        }

        public static int4 NextInt4(int4 min, int4 max)
        {
            return Thread.NextInt4(min, max);
        }

        public static uint NextUInt()
        {
            return Thread.NextUInt();
        }

        public static uint2 NextUInt2()
        {
            return Thread.NextUInt2();
        }

        public static uint3 NextUInt3()
        {
            return Thread.NextUInt3();
        }

        public static uint4 NextUInt4()
        {
            return Thread.NextUInt4();
        }

        public static uint NextUInt(uint max)
        {
            return Thread.NextUInt(max);
        }

        public static uint2 NextUInt2(uint2 max)
        {
            return Thread.NextUInt2(max);
        }

        public static uint3 NextUInt3(uint3 max)
        {
            return Thread.NextUInt3(max);
        }

        public static uint4 NextUInt4(uint4 max)
        {
            return Thread.NextUInt4(max);
        }

        public static uint NextUInt(uint min, uint max)
        {
            return Thread.NextUInt(min, max);
        }

        public static uint2 NextUInt2(uint2 min, uint2 max)
        {
            return Thread.NextUInt2(min, max);
        }

        public static uint3 NextUInt3(uint3 min, uint3 max)
        {
            return Thread.NextUInt3(min, max);
        }

        public static uint4 NextUInt4(uint4 min, uint4 max)
        {
            return Thread.NextUInt4(min, max);
        }

        public static float NextFloat()
        {
            return Thread.NextFloat();
        }

        public static float2 NextFloat2()
        {
            return Thread.NextFloat2();
        }

        public static float3 NextFloat3()
        {
            return Thread.NextFloat3();
        }

        public static float4 NextFloat4()
        {
            return Thread.NextFloat4();
        }

        public static float NextFloat(float max)
        {
            return Thread.NextFloat(max);
        }

        public static float2 NextFloat2(float2 max)
        {
            return Thread.NextFloat2(max);
        }

        public static float3 NextFloat3(float3 max)
        {
            return Thread.NextFloat3(max);
        }

        public static float4 NextFloat4(float4 max)
        {
            return Thread.NextFloat4(max);
        }

        public static float NextFloat(float min, float max)
        {
            return Thread.NextFloat(min, max);
        }

        public static float2 NextFloat2(float2 min, float2 max)
        {
            return Thread.NextFloat2(min, max);
        }

        public static float3 NextFloat3(float3 min, float3 max)
        {
            return Thread.NextFloat3(min, max);
        }

        public static float4 NextFloat4(float4 min, float4 max)
        {
            return Thread.NextFloat4(min, max);
        }

        public static double NextDouble()
        {
            return Thread.NextDouble();
        }

        public static double2 NextDouble2()
        {
            return Thread.NextDouble2();
        }

        public static double3 NextDouble3()
        {
            return Thread.NextDouble3();
        }

        public static double4 NextDouble4()
        {
            return Thread.NextDouble4();
        }

        public static double NextDouble(double max)
        {
            return Thread.NextDouble(max);
        }

        public static double2 NextDouble2(double2 max)
        {
            return Thread.NextDouble2(max);
        }

        public static double3 NextDouble3(double3 max)
        {
            return Thread.NextDouble3(max);
        }

        public static double4 NextDouble4(double4 max)
        {
            return Thread.NextDouble4(max);
        }

        public static double NextDouble(double min, double max)
        {
            return Thread.NextDouble(min, max);
        }

        public static double2 NextDouble2(double2 min, double2 max)
        {
            return Thread.NextDouble2(min, max);
        }

        public static double3 NextDouble3(double3 min, double3 max)
        {
            return Thread.NextDouble3(min, max);
        }

        public static double4 NextDouble4(double4 min, double4 max)
        {
            return Thread.NextDouble4(min, max);
        }

        public static float2 NextFloat2Direction()
        {
            return Thread.NextFloat2Direction();
        }

        public static float3 NextFloat3Direction()
        {
            return Thread.NextFloat3Direction();
        }

        public static double2 NextDouble2Direction()
        {
            return Thread.NextDouble2Direction();
        }

        public static double3 NextDouble3Direction()
        {
            return Thread.NextDouble3Direction();
        }

        public static quaternion NextQuaternionRotation()
        {
            return Thread.NextQuaternionRotation();
        }

        [OnCodeLoaded]
        private static void Initialize()
        {
            ThreadRandoms.Data = new ThreadRandom((uint)UnityEngine.Random.Range(0, int.MaxValue), Allocator.Persistent);
        }

        [OnCodeUnloading]
        private static void Shutdown()
        {
            ThreadRandoms.Data.Dispose();
        }

        private struct RandomType
        {
        }
    }
}
