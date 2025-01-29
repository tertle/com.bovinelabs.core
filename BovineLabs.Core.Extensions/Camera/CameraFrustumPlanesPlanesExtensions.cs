// <copyright file="CameraFrustumPlanesPlanesExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA

namespace BovineLabs.Core.Camera
{
    using Unity.Mathematics;
    using UnityEngine;

    public enum IntersectResult
    {
        /// <summary> The object is completely outside the planes. </summary>
        Out,

        /// <summary> The object is completely inside the planes. </summary>
        In,

        /// <summary> The object is partially intersecting the planes. </summary>
        Partial,
    }

    public static class CameraFrustumPlanesPlanesExtensions
    {
        public static IntersectResult Intersect(this CameraFrustumPlanes planes, AABB a)
        {
            var m = a.Center;
            var extent = a.Extents;

            var inCount = 0;
            for (var i = 0; i < 6; i++)
            {
                var normal = planes[i].xyz;
                var dist = math.dot(normal, m) + planes[i].w;
                var radius = math.dot(extent, math.abs(normal));
                if (dist + radius <= 0)
                {
                    return IntersectResult.Out;
                }

                if (dist > radius)
                {
                    inCount++;
                }
            }

            return inCount == 6 ? IntersectResult.In : IntersectResult.Partial;
        }

        public static bool AnyIntersect(this CameraFrustumPlanes planes, AABB a)
        {
            var m = a.Center;
            var extent = a.Extents;

            for (var i = 0; i < 6; i++)
            {
                var normal = planes[i].xyz;
                var dist = math.dot(normal, m) + planes[i].w;
                var radius = math.dot(extent, math.abs(normal));
                if (dist + radius <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static float3 GetNearCenter(this CameraFrustumPlanes planes)
        {
            // Calculate the four corner points of the near plane rectangle using plane intersections
            var nearTopLeft = IntersectThreePlanes(planes.Near, planes.Left, planes.Top);
            var nearTopRight = IntersectThreePlanes(planes.Near, planes.Right, planes.Top);
            var nearBottomLeft = IntersectThreePlanes(planes.Near, planes.Left, planes.Bottom);
            var nearBottomRight = IntersectThreePlanes(planes.Near, planes.Right, planes.Bottom);

            // Calculate the center of the near plane by averaging the corner points
            var nearPlaneCenter = (nearTopLeft + nearTopRight + nearBottomLeft + nearBottomRight) / 4f;
            return nearPlaneCenter;
        }

        // Helper method to find the intersection point of three planes
        private static float3 IntersectThreePlanes(float4 p1, float4 p2, float4 p3)
        {
            // Calculate the cross products of the planes' normals
            var cross12 = math.cross(p1.xyz, p2.xyz);
            var cross23 = math.cross(p2.xyz, p3.xyz);
            var cross31 = math.cross(p3.xyz, p1.xyz);

            // Calculate the determinant
            var determinant = math.dot(p1.xyz, cross23);
            if (math.abs(determinant) < math.EPSILON)
            {
                Debug.LogError("Planes do not intersect properly (parallel or coincident planes).");
                return float3.zero;
            }

            // Calculate the intersection point using Cramer's rule
            var point = ((-p1.w * cross23) - (p2.w * cross31) - (p3.w * cross12)) / determinant;
            return point;
        }
    }
}
#endif
