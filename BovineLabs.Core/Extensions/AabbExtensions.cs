// <copyright file="AabbExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using Unity.Physics;

    public static class AabbExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Shrink(this ref Aabb aabb, float amount)
        {
            aabb.Min += amount;
            aabb.Max -= amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ShrinkSafe(this ref Aabb aabb, float amount)
        {
            amount = math.abs(amount);
            aabb.Shrink(amount);
            EnsureSafe(ref aabb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpandX(this ref Aabb aabb, float amount)
        {
            aabb.Min.x -= amount;
            aabb.Max.x += amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpandY(this ref Aabb aabb, float amount)
        {
            aabb.Min.y -= amount;
            aabb.Max.y += amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpandZ(this ref Aabb aabb, float amount)
        {
            aabb.Min.z -= amount;
            aabb.Max.z += amount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureSafe(this ref Aabb aabb)
        {
            // Do not allow negative extents
            var extents = math.max(aabb.Extents, float3.zero);
            var center = aabb.Center;

            aabb.Min = center - (extents / 2f);
            aabb.Max = center + (extents / 2f);
        }
    }
}
#endif
