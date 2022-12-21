// <copyright file="PhysicsExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS
namespace BovineLabs.Core.Extensions
{
    using Unity.Mathematics;
    using Unity.Physics;

    public static class PhysicsExtensions
    {
        public static bool Raycast(this Plane plane, Ray ray, out float enter)
        {
            var direction = math.normalize(ray.Displacement);

            var a = math.dot(direction, plane.Normal);
            var num = -math.dot(ray.Origin, plane.Normal) - plane.Distance;

            if (a == 0.0f)
            {
                enter = 0.0f;
                return false;
            }

            enter = num / a;
            return enter > 0.0;
        }

        public static float3 GetPoint(this Ray ray, float distance)
        {
            return ray.Origin + (math.normalize(ray.Displacement) * distance);
        }
    }
}
#endif
