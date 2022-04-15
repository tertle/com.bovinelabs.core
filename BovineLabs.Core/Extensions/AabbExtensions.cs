// <copyright file="AabbExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Extensions
{
    using Unity.Mathematics;
    using Unity.Physics;

    public static class AabbExtensions
    {
        public static void Shrink(this ref Aabb aabb, float amount)
        {
            aabb.Min += amount;
            aabb.Max -= amount;
        }

        public static void ShrinkSafe(this ref Aabb aabb, float amount)
        {
            amount = math.abs(amount);

            aabb.Shrink(amount);

            // Do not allow negative extents
            var extents = math.max(aabb.Extents, float3.zero);
            var center = aabb.Center;

            aabb.Min = center - (extents / 2f);
            aabb.Max = center + (extents / 2f);
        }
    }
}
#endif