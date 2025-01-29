// <copyright file="IntersectionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Mathematics;

    public static class IntersectionTests
    {
        // https://github.com/juj/MathGeoLib/blob/master/src/Geometry/Triangle.cpp#L630
        public static bool AABBTriangle(MinMaxAABB aabb, float3 a, float3 b, float3 c)
        {
            var tMin = math.min(a, math.min(b, c));
            var tMax = math.max(a, math.max(b, c));

            if (tMin.x >= aabb.Max.x || tMax.x <= aabb.Min.x || tMin.y >= aabb.Max.y || tMax.y <= aabb.Min.y || tMin.z >= aabb.Max.z || tMax.z <= aabb.Min.z)
            {
                return false;
            }

            var center = (aabb.Min + aabb.Max) * 0.5f;
            var h = aabb.Max - center;

            Span<float3> t = stackalloc float3[3] { b - a, c - a, c - b };

            var ac = a - center;

            var n = math.cross(t[0], t[1]);
            var s = math.dot(n, ac);
            var r = math.abs(math.dot(h, math.abs(n)));

            if (math.abs(s) >= r)
            {
                return false;
            }

            Span<float3> at = stackalloc float3[3] { math.abs(t[0]), math.abs(t[1]), math.abs(t[2]) };

            var bc = b - center;
            var cc = c - center;

            // eX <cross> t[0]
            var d1 = (t[0].y * ac.z) - (t[0].z * ac.y);
            var d2 = (t[0].y * cc.z) - (t[0].z * cc.y);
            var tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[0].z) + (h.z * at[0].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eX <cross> t[1]
            d1 = (t[1].y * ac.z) - (t[1].z * ac.y);
            d2 = (t[1].y * bc.z) - (t[1].z * bc.y);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[1].z) + (h.z * at[1].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eX <cross> t[2]
            d1 = (t[2].y * ac.z) - (t[2].z * ac.y);
            d2 = (t[2].y * bc.z) - (t[2].z * bc.y);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[2].z) + (h.z * at[2].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[0]
            d1 = (t[0].z * ac.x) - (t[0].x * ac.z);
            d2 = (t[0].z * cc.x) - (t[0].x * cc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[0].z) + (h.z * at[0].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[1]
            d1 = (t[1].z * ac.x) - (t[1].x * ac.z);
            d2 = (t[1].z * bc.x) - (t[1].x * bc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[1].z) + (h.z * at[1].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eY <cross> t[2]
            d1 = (t[2].z * ac.x) - (t[2].x * ac.z);
            d2 = (t[2].z * bc.x) - (t[2].x * bc.z);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.x * at[2].z) + (h.z * at[2].x));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[0]
            d1 = (t[0].x * ac.y) - (t[0].y * ac.x);
            d2 = (t[0].x * cc.y) - (t[0].y * cc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[0].x) + (h.x * at[0].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[1]
            d1 = (t[1].x * ac.y) - (t[1].y * ac.x);
            d2 = (t[1].x * bc.y) - (t[1].y * bc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[1].x) + (h.x * at[1].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // eZ <cross> t[2]
            d1 = (t[2].x * ac.y) - (t[2].y * ac.x);
            d2 = (t[2].x * bc.y) - (t[2].y * bc.x);
            tc = (d1 + d2) * 0.5f;
            r = math.abs((h.y * at[2].x) + (h.x * at[2].y));

            if (r + math.abs(tc - d1) < math.abs(tc))
            {
                return false;
            }

            // No separating axis exists, the AABB and triangle intersect.
            return true;
        }
    }
}
