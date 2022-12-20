// <copyright file="PolygonUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Mathematics;

    public static class PolygonUtility
    {
        /// <summary> Calculates the signed area of a polygon. </summary>
        /// <param name="points"> Polygon array. </param>
        /// <returns> </returns>
        public static float SignedArea(NativeArray<float2> points)
        {
            if (points.Length <= 1)
            {
                return 0;
            }

            // https://stackoverflow.com/a/1165943
            // Sum over the edges, (x2 − x1)(y2 + y1).
            // If the result is positive the curve is clockwise, if it's negative the curve is counter-clockwise.
            // (The result is twice the enclosed area, with a +/- convention.)
            float sum = 0;

            for (var i = 0; i < points.Length - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];

                sum += (p2.x - p1.x) * (p2.y + p1.y);
            }

            {
                var p1 = points[^1];
                var p2 = points[0];

                sum += (p2.x - p1.x) * (p2.y + p1.y);
            }

            return sum;
        }

        /// <summary> Calculates the signed area of a polygon. </summary>
        /// <param name="points"> Polygon array. </param>
        /// <returns> </returns>
        public static float SignedArea(NativeArray<float3> points)
        {
            if (points.Length <= 1)
            {
                return 0;
            }

            // https://stackoverflow.com/a/1165943
            // Sum over the edges, (x2 − x1)(y2 + y1).
            // If the result is positive the curve is clockwise, if it's negative the curve is counter-clockwise.
            // (The result is twice the enclosed area, with a +/- convention.)
            float sum = 0;

            for (var i = 0; i < points.Length - 1; i++)
            {
                var p1 = points[i];
                var p2 = points[i + 1];

                sum += (p2.x - p1.x) * (p2.z + p1.z);
            }

            {
                var p1 = points[^1];
                var p2 = points[0];

                sum += (p2.x - p1.x) * (p2.z + p1.z);
            }

            return sum;
        }

        public static bool IsClockwise(NativeArray<float2> points)
        {
            // Positive value is clockwise, negative value is not
            return SignedArea(points) > 0;
        }

        public static bool IsCounterClockwise(NativeArray<float2> points)
        {
            // Positive value is clockwise, negative value is not
            return SignedArea(points) < 0;
        }

        public static bool IsClockwise(NativeArray<float3> points)
        {
            // Positive value is clockwise, negative value is not
            return SignedArea(points) > 0;
        }

        public static bool IsCounterClockwise(NativeArray<float3> points)
        {
            // Positive value is clockwise, negative value is not
            return SignedArea(points) < 0;
        }
    }
}
