// <copyright file="ConvexHullBuilder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// Based off https://github.com/OskarSigvardsson/unity-quickhull/blob/master/Scripts/ConvexHullCalculator.cs
//
// Copyright 2019 Oskar Sigvardsson
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;

    [BurstCompile]
    public static class ConvexHullBuilder
    {
        /// <summary>
        /// Constant representing a point that has yet to be assigned to a
        /// face. It's only used immediately after constructing the seed hull.
        /// </summary>
        private const int Unassigned = -2;

        /// <summary>
        /// Constant representing a point that is inside the convex hull, and
        /// thus is behind all faces. In the openSet array, all points with
        /// INSIDE are at the end of the array, with indexes larger
        /// openSetTail.
        /// </summary>
        private const int Inside = -1;

        [BurstCompile]
        public static void Generate(ref NativeArray<float3> points, ref NativeList<float3> outVerts, ref NativeList<int> outTris)
        {
            switch (points.Length)
            {
                case < 3:
                    return;
                case 3:
                    outVerts.AddRange(points);
                    outTris.Add(0);
                    outTris.Add(1);
                    outTris.Add(2);
                    return;
            }

            var data = new Data(Allocator.Temp);
            GenerateInitialHull(ref data, points);

            while (data.OpenSetTail >= 0)
            {
                GrowHull(ref data, points);
            }

            ExportMesh(ref data, points, outVerts, outTris);
        }

        /// <summary>
        /// Create initial seed hull.
        /// </summary>
        private static void GenerateInitialHull(ref Data data, NativeArray<float3> points)
        {
            // Find points suitable for use as the seed hull. Some varieties of
            // this algorithm pick extreme points here, but I'm not convinced
            // you gain all that much from that. Currently what it does is just
            // find the first four points that are not coplanar.
            int b0, b1, b2, b3;
            FindInitialHullIndices(points, out b0, out b1, out b2, out b3);

            var v0 = points[b0];
            var v1 = points[b1];
            var v2 = points[b2];
            var v3 = points[b3];

            var above = math.dot(v3 - v1, math.cross(v1 - v0, v2 - v0)) > 0.0f;

            // Create the faces of the seed hull. You need to draw a diagram
            // here, otherwise it's impossible to know what's going on :)

            // Basically: there are two different possible start-tetrahedrons,
            // depending on whether the fourth point is above or below the base
            // triangle. If you draw a tetrahedron with these coordinates (in a
            // right-handed coordinate-system):
            //   b0 = (0,0,0)
            //   b1 = (1,0,0)
            //   b2 = (0,1,0)
            //   b3 = (0,0,1)

            // you can see the first case (set b3 = (0,0,-1) for the second
            // case). The faces are added with the proper references to the
            // faces opposite each vertex
            data.FaceCount = 0;
            if (above)
            {
                data.Faces[data.FaceCount++] = new Face(b0, b2, b1, 3, 1, 2, Normal(points[b0], points[b2], points[b1]));
                data.Faces[data.FaceCount++] = new Face(b0, b1, b3, 3, 2, 0, Normal(points[b0], points[b1], points[b3]));
                data.Faces[data.FaceCount++] = new Face(b0, b3, b2, 3, 0, 1, Normal(points[b0], points[b3], points[b2]));
                data.Faces[data.FaceCount++] = new Face(b1, b2, b3, 2, 1, 0, Normal(points[b1], points[b2], points[b3]));
            }
            else
            {
                data.Faces[data.FaceCount++] = new Face(b0, b1, b2, 3, 2, 1, Normal(points[b0], points[b1], points[b2]));
                data.Faces[data.FaceCount++] = new Face(b0, b3, b1, 3, 0, 2, Normal(points[b0], points[b3], points[b1]));
                data.Faces[data.FaceCount++] = new Face(b0, b2, b3, 3, 1, 0, Normal(points[b0], points[b2], points[b3]));
                data.Faces[data.FaceCount++] = new Face(b1, b3, b2, 2, 0, 1, Normal(points[b1], points[b3], points[b2]));
            }

            // VerifyFaces(points);

            // Create the openSet. Add all points except the points of the seed
            // hull.
            for (var i = 0; i < points.Length; i++)
            {
                if (i == b0 || i == b1 || i == b2 || i == b3)
                {
                    continue;
                }

                data.OpenSet.Add(new PointFace(i, Unassigned, 0.0f));
            }

            // Add the seed hull verts to the tail of the list.
            data.OpenSet.Add(new PointFace(b0, Inside, float.NaN));
            data.OpenSet.Add(new PointFace(b1, Inside, float.NaN));
            data.OpenSet.Add(new PointFace(b2, Inside, float.NaN));
            data.OpenSet.Add(new PointFace(b3, Inside, float.NaN));

            // Set the openSetTail value. Last item in the array is
            // openSet.Count - 1, but four of the points (the verts of the seed
            // hull) are part of the closed set, so move openSetTail to just
            // before those.
            data.OpenSetTail = data.OpenSet.Length - 5;

            Check.Assume(data.OpenSet.Length == points.Length);

            // Assign all points of the open set. This does basically the same
            // thing as ReassignPoints()
            for (var i = 0; i <= data.OpenSetTail; i++)
            {
                Check.Assume(data.OpenSet[i].Face == Unassigned);
                Check.Assume(data.OpenSet[data.OpenSetTail].Face == Unassigned);
                Check.Assume(data.OpenSet[data.OpenSetTail + 1].Face == Inside);

                var assigned = false;
                var fp = data.OpenSet[i];

                Check.Assume(data.Faces.Count == 4);
                Check.Assume(data.Faces.Count == data.FaceCount);
                for (var j = 0; j < 4; j++)
                {
                    Check.Assume(data.Faces.ContainsKey(j));

                    var face = data.Faces[j];

                    var dist = PointFaceDistance(points[fp.Point], points[face.Vertex0], face.Normal);

                    if (dist > 0)
                    {
                        fp.Face = j;
                        fp.Distance = dist;
                        data.OpenSet[i] = fp;

                        assigned = true;

                        break;
                    }
                }

                if (!assigned)
                {
                    // Point is inside
                    fp.Face = Inside;
                    fp.Distance = float.NaN;

                    // Point is inside seed hull: swap point with tail, and move
                    // openSetTail back. We also have to decrement i, because
                    // there's a new item at openSet[i], and we need to process
                    // it next iteration
                    data.OpenSet[i] = data.OpenSet[data.OpenSetTail];
                    data.OpenSet[data.OpenSetTail] = fp;

                    data.OpenSetTail -= 1;
                    i -= 1;
                }
            }
        }

        /// <summary> Find four points in the point cloud that are not coplanar for the seed hull. </summary>
        private static void FindInitialHullIndices(NativeArray<float3> points, out int b0, out int b1, out int b2, out int b3)
        {
            var count = points.Length;

            for (var i0 = 0; i0 < count - 3; i0++)
            {
                for (var i1 = i0 + 1; i1 < count - 2; i1++)
                {
                    var p0 = points[i0];
                    var p1 = points[i1];

                    if (AreCoincident(p0, p1))
                    {
                        continue;
                    }

                    for (var i2 = i1 + 1; i2 < count - 1; i2++)
                    {
                        var p2 = points[i2];

                        if (AreCollinear(p0, p1, p2))
                        {
                            continue;
                        }

                        for (var i3 = i2 + 1; i3 < count - 0; i3++)
                        {
                            var p3 = points[i3];

                            if (AreCoplanar(p0, p1, p2, p3))
                            {
                                continue;
                            }

                            b0 = i0;
                            b1 = i1;
                            b2 = i2;
                            b3 = i3;

                            return;
                        }
                    }
                }
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new ArgumentException("Can't generate hull, points are coplanar");
#else
            b0 = 0;
            b1 = 0;
            b2 = 0;
            b3 = 0;
#endif
        }

        /// <summary>
        /// Grow the hull. This method takes the current hull, and expands it
        /// to encompass the point in openSet with the point furthest away
        /// from its face.
        /// </summary>
        private static void GrowHull(ref Data data, NativeArray<float3> points)
        {
            Check.Assume(data.OpenSetTail >= 0);
            Check.Assume(data.OpenSet[0].Face != Inside);

            // Find farthest point and first lit face.
            var farthestPoint = 0;
            var dist = data.OpenSet[0].Distance;

            for (var i = 1; i <= data.OpenSetTail; i++)
            {
                if (data.OpenSet[i].Distance > dist)
                {
                    farthestPoint = i;
                    dist = data.OpenSet[i].Distance;
                }
            }

            // Use lit face to find horizon and the rest of the lit
            // faces.
            FindHorizon(ref data, points, points[data.OpenSet[farthestPoint].Point], data.OpenSet[farthestPoint].Face,
                data.Faces[data.OpenSet[farthestPoint].Face]);

            // VerifyHorizon();

            // Construct new cone from horizon
            ConstructCone(ref data, points, data.OpenSet[farthestPoint].Point);

            // VerifyFaces(points);

            // Reassign points
            ReassignPoints(ref data, points);
        }

        /// <summary>
        /// Start the search for the horizon.
        /// The search is a DFS search that searches neighboring triangles in
        /// a counter-clockwise fashion. When it find a neighbor which is not
        /// lit, that edge will be a line on the horizon. If the search always
        /// proceeds counter-clockwise, the edges of the horizon will be found
        /// in counter-clockwise order.
        /// The heart of the search can be found in the recursive
        /// SearchHorizon() method, but the the first iteration of the search
        /// is special, because it has to visit three neighbors (all the
        /// neighbors of the initial triangle), while the rest of the search
        /// only has to visit two (because one of them has already been
        /// visited, the one you came from).
        /// </summary>
        private static void FindHorizon(ref Data data, NativeArray<float3> points, float3 point, int fi, Face face)
        {
            // TODO should I use epsilon in the PointFaceDistance comparisons?

            data.LitFaces.Clear();
            data.Horizon.Clear();

            data.LitFaces.Add(fi);

            Check.Assume(PointFaceDistance(point, points[face.Vertex0], face.Normal) > 0.0f);

            // For the rest of the recursive search calls, we first check if the
            // triangle has already been visited and is part of litFaces.
            // However, in this first call we can skip that because we know it
            // can't possibly have been visited yet, since the only thing in
            // litFaces is the current triangle.
            {
                var oppositeFace = data.Faces[face.Opposite0];

                var dist = PointFaceDistance(point, points[oppositeFace.Vertex0], oppositeFace.Normal);

                if (dist <= 0.0f)
                {
                    data.Horizon.Add(new HorizonEdge
                    {
                        Face = face.Opposite0,
                        Edge0 = face.Vertex1,
                        Edge1 = face.Vertex2,
                    });
                }
                else
                {
                    SearchHorizon(ref data, points, point, fi, face.Opposite0, oppositeFace);
                }
            }

            if (!data.LitFaces.Contains(face.Opposite1))
            {
                var oppositeFace = data.Faces[face.Opposite1];

                var dist = PointFaceDistance(point, points[oppositeFace.Vertex0], oppositeFace.Normal);

                if (dist <= 0.0f)
                {
                    data.Horizon.Add(new HorizonEdge
                    {
                        Face = face.Opposite1,
                        Edge0 = face.Vertex2,
                        Edge1 = face.Vertex0,
                    });
                }
                else
                {
                    SearchHorizon(ref data, points, point, fi, face.Opposite1, oppositeFace);
                }
            }

            if (!data.LitFaces.Contains(face.Opposite2))
            {
                var oppositeFace = data.Faces[face.Opposite2];

                var dist = PointFaceDistance(point, points[oppositeFace.Vertex0], oppositeFace.Normal);

                if (dist <= 0.0f)
                {
                    data.Horizon.Add(new HorizonEdge
                    {
                        Face = face.Opposite2,
                        Edge0 = face.Vertex0,
                        Edge1 = face.Vertex1,
                    });
                }
                else
                {
                    SearchHorizon(ref data, points, point, fi, face.Opposite2, oppositeFace);
                }
            }
        }

        /// <summary>
        /// Recursively search to find the horizon or lit set.
        /// </summary>
        private static void SearchHorizon(ref Data data, NativeArray<float3> points, float3 point, int prevFaceIndex, int faceCount, Face face)
        {
            Check.Assume(prevFaceIndex >= 0);
            Check.Assume(data.LitFaces.Contains(prevFaceIndex));
            Check.Assume(!data.LitFaces.Contains(faceCount));
            Check.Assume(data.Faces[faceCount].Equals(face));

            data.LitFaces.Add(faceCount);

            // Use prevFaceIndex to determine what the next face to search will
            // be, and what edges we need to cross to get there. It's important
            // that the search proceeds in counter-clockwise order from the
            // previous face.
            int nextFaceIndex0;
            int nextFaceIndex1;
            int edge0;
            int edge1;
            int edge2;

            if (prevFaceIndex == face.Opposite0)
            {
                nextFaceIndex0 = face.Opposite1;
                nextFaceIndex1 = face.Opposite2;

                edge0 = face.Vertex2;
                edge1 = face.Vertex0;
                edge2 = face.Vertex1;
            }
            else if (prevFaceIndex == face.Opposite1)
            {
                nextFaceIndex0 = face.Opposite2;
                nextFaceIndex1 = face.Opposite0;

                edge0 = face.Vertex0;
                edge1 = face.Vertex1;
                edge2 = face.Vertex2;
            }
            else
            {
                Check.Assume(prevFaceIndex == face.Opposite2);

                nextFaceIndex0 = face.Opposite0;
                nextFaceIndex1 = face.Opposite1;

                edge0 = face.Vertex1;
                edge1 = face.Vertex2;
                edge2 = face.Vertex0;
            }

            if (!data.LitFaces.Contains(nextFaceIndex0))
            {
                var oppositeFace = data.Faces[nextFaceIndex0];

                var dist = PointFaceDistance(point, points[oppositeFace.Vertex0], oppositeFace.Normal);

                if (dist <= 0.0f)
                {
                    data.Horizon.Add(new HorizonEdge
                    {
                        Face = nextFaceIndex0,
                        Edge0 = edge0,
                        Edge1 = edge1,
                    });
                }
                else
                {
                    SearchHorizon(ref data, points, point, faceCount, nextFaceIndex0, oppositeFace);
                }
            }

            if (!data.LitFaces.Contains(nextFaceIndex1))
            {
                var oppositeFace = data.Faces[nextFaceIndex1];

                var dist = PointFaceDistance(point, points[oppositeFace.Vertex0], oppositeFace.Normal);

                if (dist <= 0.0f)
                {
                    data.Horizon.Add(new HorizonEdge
                    {
                        Face = nextFaceIndex1,
                        Edge0 = edge1,
                        Edge1 = edge2,
                    });
                }
                else
                {
                    SearchHorizon(ref data, points, point, faceCount, nextFaceIndex1, oppositeFace);
                }
            }
        }

        /// <summary>
        /// Remove all lit faces and construct new faces from the horizon in a
        /// "cone-like" fashion.
        /// This is a relatively straight-forward procedure, given that the
        /// horizon is handed to it in already sorted counter-clockwise. The
        /// neighbors of the new faces are easy to find: they're the previous
        /// and next faces to be constructed in the cone, as well as the face
        /// on the other side of the horizon. We also have to update the face
        /// on the other side of the horizon to reflect it's new neighbor from
        /// the cone.
        /// </summary>
        private static void ConstructCone(ref Data data, NativeArray<float3> points, int farthestPoint)
        {
            foreach (var fi in data.LitFaces)
            {
                Check.Assume(data.Faces.ContainsKey(fi));
                data.Faces.Remove(fi);
            }

            var firstNewFace = data.FaceCount;

            for (var i = 0; i < data.Horizon.Length; i++)
            {
                // Vertices of the new face, the farthest point as well as the
                // edge on the horizon. Horizon edge is CCW, so the triangle
                // should be as well.
                var v0 = farthestPoint;
                var v1 = data.Horizon[i].Edge0;
                var v2 = data.Horizon[i].Edge1;

                // Opposite faces of the triangle. First, the edge on the other
                // side of the horizon, then the next/prev faces on the new cone
                var o0 = data.Horizon[i].Face;
                var o1 = i == data.Horizon.Length - 1 ? firstNewFace : firstNewFace + i + 1;
                var o2 = i == 0 ? (firstNewFace + data.Horizon.Length) - 1 : (firstNewFace + i) - 1;

                var fi = data.FaceCount++;

                data.Faces[fi] = new Face(v0, v1, v2, o0, o1, o2, Normal(points[v0], points[v1], points[v2]));

                var horizonFace = data.Faces[data.Horizon[i].Face];

                if (horizonFace.Vertex0 == v1)
                {
                    Check.Assume(v2 == horizonFace.Vertex2);
                    horizonFace.Opposite1 = fi;
                }
                else if (horizonFace.Vertex1 == v1)
                {
                    Check.Assume(v2 == horizonFace.Vertex0);
                    horizonFace.Opposite2 = fi;
                }
                else
                {
                    Check.Assume(v1 == horizonFace.Vertex2);
                    Check.Assume(v2 == horizonFace.Vertex1);
                    horizonFace.Opposite0 = fi;
                }

                data.Faces[data.Horizon[i].Face] = horizonFace;
            }
        }

        /// <summary>
        /// Reassign points based on the new faces added by ConstructCone().
        /// Only points that were previous assigned to a removed face need to
        /// be updated, so check litFaces while looping through the open set.
        /// There is a potential optimization here: there's no reason to loop
        /// through the entire openSet here. If each face had it's own
        /// openSet, we could just loop through the openSets in the removed
        /// faces. That would make the loop here shorter.
        /// However, to do that, we would have to juggle A LOT more List's,
        /// and we would need an object pool to manage them all without
        /// generating a whole bunch of garbage. I don't think it's worth
        /// doing that to make this loop shorter, a straight for-loop through
        /// a list is pretty darn fast. Still, it might be worth trying.
        /// </summary>
        private static void ReassignPoints(ref Data data, NativeArray<float3> points)
        {
            for (var i = 0; i <= data.OpenSetTail; i++)
            {
                var fp = data.OpenSet[i];

                if (data.LitFaces.Contains(fp.Face))
                {
                    var assigned = false;
                    var point = points[fp.Point];

                    foreach (var kvp in data.Faces)
                    {
                        var fi = kvp.Key;
                        var face = kvp.Value;

                        var dist = PointFaceDistance(point, points[face.Vertex0], face.Normal);

                        if (dist > math.EPSILON)
                        {
                            assigned = true;

                            fp.Face = fi;
                            fp.Distance = dist;

                            data.OpenSet[i] = fp;

                            break;
                        }
                    }

                    if (!assigned)
                    {
                        // If point hasn't been assigned, then it's inside the
                        // convex hull. Swap it with openSetTail, and decrement
                        // openSetTail. We also have to decrement i, because
                        // there's now a new thing in openSet[i], so we need i
                        // to remain the same the next iteration of the loop.
                        fp.Face = Inside;
                        fp.Distance = float.NaN;

                        data.OpenSet[i] = data.OpenSet[data.OpenSetTail];
                        data.OpenSet[data.OpenSetTail] = fp;

                        i--;
                        data.OpenSetTail--;
                    }
                }
            }
        }

        /// <summary> Final step in algorithm, export the faces of the convex hull in a mesh-friendly format. </summary>
        private static void ExportMesh(ref Data data, NativeArray<float3> points, NativeList<float3> verts, NativeList<int> tris)
        {
            verts.Clear();
            tris.Clear();

            using var e = data.Faces.GetEnumerator();
            while (e.MoveNext())
            {
                var face = e.Current.Value;

                int vi0, vi1, vi2;

                if (!data.HullVerts.TryGetValue(face.Vertex0, out vi0))
                {
                    vi0 = verts.Length;
                    data.HullVerts[face.Vertex0] = vi0;
                    verts.Add(points[face.Vertex0]);
                }

                if (!data.HullVerts.TryGetValue(face.Vertex1, out vi1))
                {
                    vi1 = verts.Length;
                    data.HullVerts[face.Vertex1] = vi1;
                    verts.Add(points[face.Vertex1]);
                }

                if (!data.HullVerts.TryGetValue(face.Vertex2, out vi2))
                {
                    vi2 = verts.Length;
                    data.HullVerts[face.Vertex2] = vi2;
                    verts.Add(points[face.Vertex2]);
                }

                tris.Add(vi0);
                tris.Add(vi1);
                tris.Add(vi2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 Normal(float3 v0, float3 v1, float3 v2)
        {
            return math.normalize(math.cross(v1 - v0, v2 - v0));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float PointFaceDistance(float3 point, float3 pointOnFace, float3 normal)
        {
            return math.dot(normal, point - pointOnFace);
        }

        /// <summary> Check if two points are coincident. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreCoincident(float3 a, float3 b)
        {
            return math.distance(a, b) <= math.EPSILON;
        }

        /// <summary> Check if three points are collinear. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreCollinear(float3 a, float3 b, float3 c)
        {
            return math.length(math.cross(c - a, c - b)) <= math.EPSILON;
        }

        /// <summary> Check if four points are coplanar. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreCoplanar(float3 a, float3 b, float3 c, float3 d)
        {
            var n1 = math.cross(c - a, c - b);
            var n2 = math.cross(d - a, d - b);

            var m1 = math.length(n1);
            var m2 = math.length(n2);

            return m1 <= math.EPSILON || m2 <= math.EPSILON || AreCollinear(float3.zero, (1.0f / m1) * n1, (1.0f / m2) * n2);
        }

        private struct Data
        {
            public NativeHashMap<int, Face> Faces;

            /// <summary>
            /// The set of points to be processed. "openSet" is a misleading name,
            /// because it's both the open set (points which are still outside the
            /// convex hull) and the closed set (points that are inside the convex
            /// hull). The first part of the array (with indexes <= openSetTail)
            /// is the openSet, the last part of the array (with indexes >
            /// openSetTail) are the closed set, with Face set to INSIDE. The
            /// closed set is largely irrelevant to the algorithm, the open set is
            /// what matters.
            /// Storing the entire open set in one big list has a downside: when
            /// we're reassigning points after ConstructCone, we only need to
            /// reassign points that belong to the faces that have been removed,
            /// but storing it in one array, we have to loop through the entire
            /// list, and checking litFaces to determine which we can skip and
            /// which need to be reassigned.
            /// The alternative here is to give each face in Face array it's own
            /// openSet. I don't like that solution, because then you have to
            /// juggle so many more heap-allocated List's, we'd have to use
            /// object pools and such. It would do a lot more allocation, and it
            /// would have worse locality. I should maybe test that solution, but
            /// it probably wont be faster enough (if at all) to justify the extra
            /// allocations.
            /// </summary>
            public NativeList<PointFace> OpenSet;

            /// <summary>
            /// Set of faces which are "lit" by the current point in the set. This
            /// is used in the FindHorizon() DFS search to keep track of which
            /// faces we've already visited, and in the ReassignPoints() method to
            /// know which points need to be reassigned.
            /// </summary>
            public NativeHashSet<int> LitFaces;

            /// <summary>
            /// The current horizon. Generated by the FindHorizon() DFS search,
            /// and used in ConstructCone to construct new faces. The list of
            /// edges are in CCW order.
            /// </summary>
            public NativeList<HorizonEdge> Horizon;

            /// <summary>
            /// If SplitVerts is false, this Dictionary is used to keep track of
            /// which points we've added to the final mesh.
            /// </summary>
            public NativeHashMap<int, int> HullVerts;

            /// <summary>
            /// The "tail" of the openSet, the last index of a vertex that has
            /// been assigned to a face.
            /// </summary>
            public int OpenSetTail;

            /// <summary>
            /// When adding a new face to the faces Dictionary, use this for the
            /// key and then increment it.
            /// </summary>
            public int FaceCount;

            public Data(Allocator allocator)
            {
                this.Faces = new NativeHashMap<int, Face>(0, allocator);
                this.OpenSet = new NativeList<PointFace>(0, allocator);
                this.LitFaces = new NativeHashSet<int>(0, allocator);
                this.Horizon = new NativeList<HorizonEdge>(0, allocator);
                this.HullVerts = new NativeHashMap<int, int>(0, allocator);

                this.OpenSetTail = -1;
                this.FaceCount = 0;
            }
        }

        /// <summary>
        /// Struct representing a single face.
        /// Vertex0, Vertex1 and Vertex2 are the vertices in CCW order. They
        /// acutal points are stored in the points array, these are just
        /// indexes into that array.
        /// Opposite0, Opposite1 and Opposite2 are the keys to the faces which
        /// share an edge with this face. Opposite0 is the face opposite
        /// Vertex0 (so it has an edge with Vertex2 and Vertex1), etc.
        /// Normal is (unsurprisingly) the normal of the triangle.
        /// </summary>
        private struct Face
        {
            public readonly int Vertex0;
            public readonly int Vertex1;
            public readonly int Vertex2;

            public int Opposite0;
            public int Opposite1;
            public int Opposite2;

            public float3 Normal;

            public Face(int v0, int v1, int v2, int o0, int o1, int o2, float3 normal)
            {
                this.Vertex0 = v0;
                this.Vertex1 = v1;
                this.Vertex2 = v2;
                this.Opposite0 = o0;
                this.Opposite1 = o1;
                this.Opposite2 = o2;
                this.Normal = normal;
            }

            public bool Equals(Face other)
            {
                return this.Vertex0 == other.Vertex0 &&
                    this.Vertex1 == other.Vertex1 &&
                    this.Vertex2 == other.Vertex2 &&
                    this.Opposite0 == other.Opposite0 &&
                    this.Opposite1 == other.Opposite1 &&
                    this.Opposite2 == other.Opposite2 &&
                    this.Normal.Equals(other.Normal);
            }
        }

        /// <summary>
        /// Struct representing a mapping between a point and a face. These
        /// are used in the openSet array.
        /// Point is the index of the point in the points array, Face is the
        /// key of the face in the Key dictionary, Distance is the distance
        /// from the face to the point.
        /// </summary>
        private struct PointFace
        {
            public readonly int Point;
            public int Face;
            public float Distance;

            public PointFace(int p, int f, float d)
            {
                this.Point = p;
                this.Face = f;
                this.Distance = d;
            }
        }

        /// <summary>
        /// Struct representing a single edge in the horizon.
        /// Edge0 and Edge1 are the vertexes of edge in CCW order, Face is the
        /// face on the other side of the horizon.
        /// TODO Edge1 isn't actually needed, you can just index the next item
        /// in the horizon array.
        /// </summary>
        private struct HorizonEdge
        {
            public int Face;
            public int Edge0;
            public int Edge1;
        }
    }
}
