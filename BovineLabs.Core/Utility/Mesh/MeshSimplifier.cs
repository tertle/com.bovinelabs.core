// <copyright file="MeshSimplifier.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;

    [BurstCompile]
    public static class MeshSimplifier
    {
        private const int TriangleEdgeCount = 3;
        private const int TriangleVertexCount = 3;

        public static Result Simplify(Mesh mesh, Options options, Allocator allocator = Allocator.Temp)
        {
            if (mesh.subMeshCount > 1)
            {
                Debug.LogWarning("Only supports single sub mesh");
            }

            var vertices = mesh.vertices;
            var tris = mesh.GetTriangles(0);

            return Simplify(vertices, tris, options, allocator);
        }

        public static Result Simplify(Vector3[] verts, int[] tris, Options options, Allocator allocator = Allocator.Temp)
        {
            var trisCount = tris.Length / TriangleVertexCount;

            var triangles = new NativeList<Triangle>(trisCount, allocator);
            triangles.ResizeUninitialized(trisCount);
            var trisArr = triangles.AsArray();

            for (var j = 0; j < trisCount; j++)
            {
                var offset = j * 3;
                var v0 = tris[offset];
                var v1 = tris[offset + 1];
                var v2 = tris[offset + 2];
                var triangleIndex = j;
                trisArr[triangleIndex] = new Triangle(triangleIndex, v0, v1, v2);
            }

            var vertices = new NativeList<Vertex>(verts.Length, allocator);
            vertices.ResizeUninitialized(verts.Length);
            for (var i = 0; i < verts.Length; i++)
            {
                vertices[i] = new Vertex(i, (float3)verts[i]);
            }

            var result = new Result
            {
                Vertices = vertices,
                Triangles = triangles,
            };

            Simplify(ref result, ref options, allocator);

            return result;
        }

        public static Result Simplify(NativeArray<Vector3> verts, NativeArray<int> tris, Options options, Allocator allocator = Allocator.Temp)
        {
            var triangles = new NativeList<Triangle>(tris.Length, allocator);
            triangles.ResizeUninitialized(tris.Length);

            var trisArr = triangles.AsArray();

            var subMeshTriangleCount = tris.Length / TriangleVertexCount;
            for (var j = 0; j < subMeshTriangleCount; j++)
            {
                var offset = j * 3;
                var v0 = tris[offset];
                var v1 = tris[offset + 1];
                var v2 = tris[offset + 2];
                var triangleIndex = j;
                trisArr[triangleIndex] = new Triangle(triangleIndex, v0, v1, v2);
            }

            var vertices = new NativeList<Vertex>(verts.Length, allocator);
            vertices.ResizeUninitialized(verts.Length);
            for (var i = 0; i < verts.Length; i++)
            {
                vertices[i] = new Vertex(i, (float3)verts[i]);
            }

            var result = new Result
            {
                Vertices = vertices,
                Triangles = triangles,
            };

            Simplify(ref result, ref options, allocator);

            return result;
        }

        [BurstCompile(CompileSynchronously = true)]
        private static void Simplify(ref Result result, ref Options options, in Allocator allocator)
        {
            var refs = new NativeList<Ref>(allocator);

            options.Quality = math.clamp(options.Quality, 0, 1);

            var deletedTris = 0;
            var deleted0 = new NativeList<bool>(20, allocator);
            var deleted1 = new NativeList<bool>(20, allocator);
            var triangles = result.Triangles.AsArray();
            var triangleCount = result.Triangles.Length;
            var startTrisCount = triangleCount;
            var targetTrisCount = (int)math.round(triangleCount * options.Quality);

            for (var iteration = 0; iteration < options.MaxIterationCount; iteration++)
            {
                if (startTrisCount - deletedTris <= targetTrisCount)
                {
                    break;
                }

                // Update mesh once in a while
                if (iteration % 5 == 0)
                {
                    UpdateMesh(ref result, ref refs, iteration);
                    triangles = result.Triangles.AsArray();
                    triangleCount = result.Triangles.Length;
                }

                // Clear dirty flag
                for (var i = 0; i < triangleCount; i++)
                {
                    triangles.ElementAt(i).Dirty = false;
                }

                // All triangles with edges below the threshold will be removed
                //
                // The following numbers works well for most models.
                // If it does not, try to adjust the 3 parameters
                var threshold = 0.000000001 * math.pow(iteration + 3, options.Agressiveness);

                // Remove vertices & mark deleted triangles
                RemoveVertexPass(ref result, ref refs, startTrisCount, targetTrisCount, threshold, deleted0, deleted1, ref deletedTris);
            }

            CompactMesh(ref result);

            refs.Dispose();
            deleted0.Dispose();
            deleted1.Dispose();
        }

        /// <summary>
        /// Compact triangles, compute edge error and build reference list.
        /// </summary>
        /// <param name="iteration"> The iteration index. </param>
        private static void UpdateMesh(ref Result result, ref NativeList<Ref> refs, int iteration)
        {
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();

            var triangleCount = result.Triangles.Length;
            var vertexCount = result.Vertices.Length;
            if (iteration > 0) // compact triangles
            {
                var dst = 0;
                for (var i = 0; i < triangleCount; i++)
                {
                    if (!triangles[i].Deleted)
                    {
                        if (dst != i)
                        {
                            triangles[dst] = triangles[i];
                            triangles.ElementAt(dst).Index = dst;
                        }

                        dst++;
                    }
                }

                result.Triangles.Resize(dst, NativeArrayOptions.UninitializedMemory);
                triangles = result.Triangles.AsArray();
                triangleCount = dst;
            }

            UpdateReferences(ref result, ref refs);

            // Identify boundary : vertices[].border=0,1
            if (iteration == 0)
            {
                var refArr = refs.AsArray();

                var vcount = new NativeList<int>(8, Allocator.Temp); // TODO reuse
                var vids = new NativeList<int>(8, Allocator.Temp);
                int vsize;
                for (var i = 0; i < vertexCount; i++)
                {
                    ref var v = ref vertices.ElementAt(i);
                    v.BorderEdge = false;
                    v.UVSeamEdge = false;
                    v.UVFoldoverEdge = false;
                }

                for (var i = 0; i < vertexCount; i++)
                {
                    var tstart = vertices[i].TStart;
                    var tcount = vertices[i].Tcount;
                    vcount.Clear();
                    vids.Clear();
                    vsize = 0;

                    int id;
                    for (var j = 0; j < tcount; j++)
                    {
                        var tid = refArr[tstart + j].TId;
                        for (var k = 0; k < TriangleVertexCount; k++)
                        {
                            var ofs = 0;
                            id = triangles[tid][k];
                            while (ofs < vsize)
                            {
                                if (vids[ofs] == id)
                                {
                                    break;
                                }

                                ++ofs;
                            }

                            if (ofs == vsize)
                            {
                                vcount.Add(1);
                                vids.Add(id);
                                ++vsize;
                            }
                            else
                            {
                                ++vcount[ofs];
                            }
                        }
                    }

                    for (var j = 0; j < vsize; j++)
                    {
                        if (vcount[j] == 1)
                        {
                            id = vids[j];
                            vertices.ElementAt(id).BorderEdge = true;
                        }
                    }
                }

                // Init Quadrics by Plane & Edge Errors
                //
                // required at the beginning ( iteration == 0 )
                // recomputing during the simplification is not required,
                // but mostly improves the result for closed meshes
                for (var i = 0; i < vertexCount; i++)
                {
                    vertices.ElementAt(i).Q = default;
                }

                for (var i = 0; i < triangleCount; i++)
                {
                    var v = triangles[i].V;

                    var p0 = vertices[v.x].P;
                    var p1 = vertices[v.y].P;
                    var p2 = vertices[v.z].P;
                    var p10 = p1 - p0;
                    var p20 = p2 - p0;
                    var n = math.cross(p10, p20);
                    n = math.normalizesafe(n);
                    triangles.ElementAt(i).N = (float3)n;

                    var sm = new SymmetricMatrix(n.x, n.y, n.z, -math.dot(n, p0));
                    vertices.ElementAt(v.x).Q += sm;
                    vertices.ElementAt(v.y).Q += sm;
                    vertices.ElementAt(v.z).Q += sm;
                }

                for (var i = 0; i < triangleCount; i++)
                {
                    // Calc Edge Error
                    // var triangle = triangles[i];
                    ref var triangle = ref triangles.ElementAt(i);

                    var err0 = CalculateError(ref vertices.ElementAt(triangle.V.x), ref vertices.ElementAt(triangle.V.y), out _);
                    var err1 = CalculateError(ref vertices.ElementAt(triangle.V.y), ref vertices.ElementAt(triangle.V.z), out _);
                    var err2 = CalculateError(ref vertices.ElementAt(triangle.V.z), ref vertices.ElementAt(triangle.V.x), out _);

                    triangle.Err0 = err0;
                    triangle.Err1 = err1;
                    triangle.Err2 = err2;
                    triangle.Err3 = math.cmin(new double3(err0, err1, err2));
                }
            }
        }

        private static void RemoveVertexPass(
            ref Result result, ref NativeList<Ref> refs, int startTrisCount, int targetTrisCount, double threshold, NativeList<bool> deleted0,
            NativeList<bool> deleted1, ref int deletedTris)
        {
            var triangles = result.Triangles.AsArray();
            var triangleCount = result.Triangles.Length;
            var vertices = result.Vertices.AsArray();

            for (var tid = 0; tid < triangleCount; tid++)
            {
                if (triangles[tid].Dirty || triangles[tid].Deleted || triangles[tid].Err3 > threshold)
                {
                    continue;
                }

                var errArr = triangles[tid].GetErrors();
                var attributeIndexArr = triangles[tid].GetAttributeIndices();
                for (var edgeIndex = 0; edgeIndex < TriangleEdgeCount; edgeIndex++)
                {
                    if (errArr[edgeIndex] > threshold)
                    {
                        continue;
                    }

                    var nextEdgeIndex = (edgeIndex + 1) % TriangleEdgeCount;
                    var i0 = triangles[tid][edgeIndex];
                    var i1 = triangles[tid][nextEdgeIndex];

                    // Border check
                    if (vertices[i0].BorderEdge != vertices[i1].BorderEdge)
                    {
                        continue;
                    }

                    // Seam check
                    if (vertices[i0].UVSeamEdge != vertices[i1].UVSeamEdge)
                    {
                        continue;
                    }

                    // Foldover check
                    if (vertices[i0].UVFoldoverEdge != vertices[i1].UVFoldoverEdge)
                    {
                        continue;
                    }

                    // If borders should be preserved
                    // if ( /*simplificationOptions.PreserveBorderEdges && */vertices[i0].borderEdge)
                    // {
                    //     continue;
                    // }
                    // // If seams should be preserved
                    // else if (simplificationOptions.PreserveUVSeamEdges && vertices[i0].uvSeamEdge)
                    //     continue;
                    // // If foldovers should be preserved
                    // else if (simplificationOptions.PreserveUVFoldoverEdges && vertices[i0].uvFoldoverEdge)
                    //     continue;

                    // Compute vertex to collapse to
                    CalculateError(ref vertices.ElementAt(i0), ref vertices.ElementAt(i1), out var p);
                    deleted0.Resize(vertices[i0].Tcount, NativeArrayOptions.UninitializedMemory); // normals temporarily
                    deleted1.Resize(vertices[i1].Tcount, NativeArrayOptions.UninitializedMemory); // normals temporarily

                    // Don't remove if flipped
                    if (Flipped(ref result, ref refs, ref p, i0, i1, ref vertices.ElementAt(i0), deleted0.AsArray()))
                    {
                        continue;
                    }

                    if (Flipped(ref result, ref refs, ref p, i1, i0, ref vertices.ElementAt(i1), deleted1.AsArray()))
                    {
                        continue;
                    }

                    // Not flipped, so remove edge
                    vertices.ElementAt(i0).P = p;
                    vertices.ElementAt(i0).Q += vertices[i1].Q;

                    // Interpolate the vertex attributes
                    var ia0 = attributeIndexArr[edgeIndex];

                    if (vertices[i0].UVSeamEdge)
                    {
                        ia0 = -1;
                    }

                    var tstart = refs.Length;

                    UpdateTriangles(ref result, ref refs, i0, ia0, ref vertices.ElementAt(i0), deleted0, ref deletedTris);
                    UpdateTriangles(ref result, ref refs, i0, ia0, ref vertices.ElementAt(i1), deleted1, ref deletedTris);

                    var tcount = refs.Length - tstart;
                    if (tcount <= vertices[i0].Tcount)
                    {
                        // save ram
                        if (tcount > 0)
                        {
                            var refsArr = refs.AsArray();

                            // TODO does this need a memmove?
                            var src = refsArr.GetSubArray(tstart, tcount);
                            var dst = refsArr.GetSubArray(vertices[i0].TStart, tcount);
                            // refsArr.GetSubArray(tstart, tcount).CopyTo(refsArr.GetSubArray(vertices[i0].tstart, tcount));

                            unsafe
                            {
                                // TODO pretty sure this is safe but need to confirm MemMove isn't required
                                UnsafeUtility.MemCpy(dst.GetUnsafePtr(), src.GetUnsafeReadOnlyPtr(), tcount * sizeof(Ref));
                            }
                        }
                    }
                    else
                    {
                        // append
                        vertices.ElementAt(i0).TStart = tstart;
                    }

                    vertices.ElementAt(i0).Tcount = tcount;
                    break;
                }

                // Check if we are already done
                if (startTrisCount - deletedTris <= targetTrisCount)
                {
                    break;
                }
            }
        }

        private static void CompactMesh(ref Result result)
        {
            var dst = 0;
            var vertices = result.Vertices.AsArray();
            var vertexCount = result.Vertices.Length;
            for (var i = 0; i < vertexCount; i++)
            {
                vertices.ElementAt(i).Tcount = 0;
            }

            var triangles = result.Triangles.AsArray();
            var triangleCount = result.Triangles.Length;
            for (var i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                if (!triangle.Deleted)
                {
                    if (triangle.Va.x != triangle.V.x)
                    {
                        var iDest = triangle.Va.x;
                        var iSrc = triangle.V.x;
                        vertices.ElementAt(iDest).P = vertices[iSrc].P;
                        triangle.V.x = triangle.Va.x;
                    }

                    if (triangle.Va.y != triangle.V.y)
                    {
                        var iDest = triangle.Va.y;
                        var iSrc = triangle.V.y;
                        vertices.ElementAt(iDest).P = vertices[iSrc].P;
                        triangle.V.y = triangle.Va.y;
                    }

                    if (triangle.Va.z != triangle.V.z)
                    {
                        var iDest = triangle.Va.z;
                        var iSrc = triangle.V.z;
                        vertices.ElementAt(iDest).P = vertices[iSrc].P;
                        triangle.V.z = triangle.Va.z;
                    }

                    var newTriangleIndex = dst++;
                    triangles[newTriangleIndex] = triangle;
                    triangles.ElementAt(newTriangleIndex).Index = newTriangleIndex;

                    vertices.ElementAt(triangle.V.x).Tcount = 1;
                    vertices.ElementAt(triangle.V.y).Tcount = 1;
                    vertices.ElementAt(triangle.V.z).Tcount = 1;
                }
            }

            triangleCount = dst;

            result.Triangles.Resize(triangleCount, NativeArrayOptions.UninitializedMemory);
            triangles = result.Triangles.AsArray();

            dst = 0;
            for (var i = 0; i < vertexCount; i++)
            {
                var vert = vertices[i];
                if (vert.Tcount > 0)
                {
                    vertices.ElementAt(i).TStart = dst;

                    if (dst != i)
                    {
                        vertices.ElementAt(dst).Index = dst;
                        vertices.ElementAt(dst).P = vert.P;
                    }

                    ++dst;
                }
            }

            for (var i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                triangle.V.x = vertices[triangle.V.x].TStart;
                triangle.V.y = vertices[triangle.V.y].TStart;
                triangle.V.z = vertices[triangle.V.z].TStart;
                triangles[i] = triangle;
            }

            vertexCount = dst;
            result.Vertices.Resize(vertexCount, NativeArrayOptions.UninitializedMemory);
        }

        private static void UpdateReferences(ref Result result, ref NativeList<Ref> refs)
        {
            var triangleCount = result.Triangles.Length;
            var vertexCount = result.Vertices.Length;
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();

            // Init Reference ID list
            for (var i = 0; i < vertexCount; i++)
            {
                ref var v = ref vertices.ElementAt(i);
                v.TStart = 0;
                v.Tcount = 0;
            }

            for (var i = 0; i < triangleCount; i++)
            {
                ++vertices.ElementAt(triangles[i].V.x).Tcount;
                ++vertices.ElementAt(triangles[i].V.y).Tcount;
                ++vertices.ElementAt(triangles[i].V.z).Tcount;
            }

            var tstart = 0;
            for (var i = 0; i < vertexCount; i++)
            {
                ref var v = ref vertices.ElementAt(i);
                v.TStart = tstart;
                tstart += v.Tcount;
                v.Tcount = 0;
            }

            // Write References
            refs.ResizeUninitialized(tstart);
            var refArr = refs.AsArray();
            for (var i = 0; i < triangleCount; i++)
            {
                var v = triangles[i].V;

                ref var v0 = ref vertices.ElementAt(v.x);
                ref var v1 = ref vertices.ElementAt(v.y);
                ref var v2 = ref vertices.ElementAt(v.z);

                var start0 = v0.TStart;
                var count0 = v0.Tcount++;
                var start1 = v1.TStart;
                var count1 = v1.Tcount++;
                var start2 = v2.TStart;
                var count2 = v2.Tcount++;

                refArr.ElementAt(start0 + count0).Set(i, 0);
                refArr.ElementAt(start1 + count1).Set(i, 1);
                refArr.ElementAt(start2 + count2).Set(i, 2);
            }
        }

        private static double CalculateError(ref Vertex vert0, ref Vertex vert1, out double3 result)
        {
            // compute interpolated vertex
            var q = vert0.Q + vert1.Q;
            var borderEdge = vert0.BorderEdge && vert1.BorderEdge;
            double error;
            var det = q.Determinant1();
            if (det != 0.0 && !borderEdge)
            {
                // q_delta is invertible
                result = new double3((-1.0 / det) * q.Determinant2(), // vx = A41/det(q_delta)
                    (1.0 / det) * q.Determinant3(), // vy = A42/det(q_delta)
                    (-1.0 / det) * q.Determinant4()); // vz = A43/det(q_delta)

                // double curvatureError = 0;
                // if (simplificationOptions.PreserveSurfaceCurvature)
                // {
                //     curvatureError = CurvatureError(ref vert0, ref vert1);
                // }

                error = VertexError(ref q, result.x, result.y, result.z); // + curvatureError;
            }
            else
            {
                // det = 0 -> try to find best result
                var p1 = vert0.P;
                var p2 = vert1.P;
                var p3 = (p1 + p2) * 0.5f;
                var error1 = VertexError(ref q, p1.x, p1.y, p1.z);
                var error2 = VertexError(ref q, p2.x, p2.y, p2.z);
                var error3 = VertexError(ref q, p3.x, p3.y, p3.z);

                if (error1 < error2)
                {
                    if (error1 < error3)
                    {
                        error = error1;
                        result = p1;
                    }
                    else
                    {
                        error = error3;
                        result = p3;
                    }
                }
                else if (error2 < error3)
                {
                    error = error2;
                    result = p2;
                }
                else
                {
                    error = error3;
                    result = p3;
                }
            }

            return error;
        }

        private static double VertexError(ref SymmetricMatrix q, double x, double y, double z)
        {
            return (q.M0 * x * x) +
                (2 * q.M1 * x * y) +
                (2 * q.M2 * x * z) +
                (2 * q.M3 * x) +
                (q.M4 * y * y) +
                (2 * q.M5 * y * z) +
                (2 * q.M6 * y) +
                (q.M7 * z * z) +
                (2 * q.M8 * z) +
                q.M9;
        }

        private static bool Flipped(ref Result result, ref NativeList<Ref> refs, ref double3 p, int i0, int i1, ref Vertex v0, NativeArray<bool> deleted)
        {
            var tcount = v0.Tcount;
            var refsArr = refs.AsArray();
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();
            for (var k = 0; k < tcount; k++)
            {
                var r = refsArr[v0.TStart + k];
                if (triangles[r.TId].Deleted)
                {
                    continue;
                }

                var s = r.TVertex;
                var id1 = triangles[r.TId][(s + 1) % 3];
                var id2 = triangles[r.TId][(s + 2) % 3];
                if (id1 == i1 || id2 == i1)
                {
                    deleted[k] = true;
                    continue;
                }

                var d1 = vertices[id1].P - p;
                d1 = math.normalizesafe(d1);
                var d2 = vertices[id2].P - p;
                d2 = math.normalizesafe(d2);
                var dot = math.dot(d1, d2);
                if (Math.Abs(dot) > 0.999)
                {
                    return true;
                }

                var n = math.cross(d1, d2);
                n = math.normalizesafe(n);
                deleted[k] = false;
                dot = math.dot(n, triangles[r.TId].N);
                if (dot < 0.2)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CalculateBarycentricCoords(ref double3 point, ref double3 a, ref double3 b, ref double3 c, out Vector3 result)
        {
            const double denomEpilson = 0.00000001;

            var v0 = b - a;
            var v1 = c - a;
            var v2 = point - a;
            var d00 = math.dot(v0, v0);
            var d01 = math.dot(v0, v1);
            var d11 = math.dot(v1, v1);
            var d20 = math.dot(v2, v0);
            var d21 = math.dot(v2, v1);
            var denom = (d00 * d11) - (d01 * d01);

            // Make sure the denominator is not too small to cause math problems
            if (math.abs(denom) < denomEpilson)
            {
                denom = denomEpilson;
            }

            var v = ((d11 * d20) - (d01 * d21)) / denom;
            var w = ((d00 * d21) - (d01 * d20)) / denom;
            var u = 1.0 - v - w;
            result = new Vector3((float)u, (float)v, (float)w);
        }

        private static void UpdateTriangles(
            ref Result result, ref NativeList<Ref> refs, int i0, int ia0, ref Vertex v, NativeList<bool> deleted, ref int deletedTriangles)
        {
            var tcount = v.Tcount;
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();
            for (var k = 0; k < tcount; k++)
            {
                var r = refs[v.TStart + k];
                var tid = r.TId;
                var t = triangles[tid];
                if (t.Deleted)
                {
                    continue;
                }

                if (deleted[k])
                {
                    triangles.ElementAt(tid).Deleted = true;
                    ++deletedTriangles;
                    continue;
                }

                t[r.TVertex] = i0;
                if (ia0 != -1)
                {
                    t.SetAttributeIndex(r.TVertex, ia0);
                }

                t.Dirty = true;
                t.Err0 = CalculateError(ref vertices.ElementAt(t.V.x), ref vertices.ElementAt(t.V.y), out _);
                t.Err1 = CalculateError(ref vertices.ElementAt(t.V.y), ref vertices.ElementAt(t.V.z), out _);
                t.Err2 = CalculateError(ref vertices.ElementAt(t.V.z), ref vertices.ElementAt(t.V.x), out _);
                t.Err3 = math.cmin(new double3(t.Err0, t.Err1, t.Err2));
                triangles[tid] = t;
                refs.Add(r);
            }
        }

        [Serializable]
        public struct Options
        {
            public double Quality { get; set; }

            /// <summary>
            /// The maximum iteration count. Higher number is more expensive but can bring you closer to your target quality.
            /// Sometimes a lower maximum count might be desired in order to lower the performance cost.
            /// Default value: 100
            /// </summary>
            [Tooltip("The maximum iteration count. Higher number is more expensive but can bring you closer to your target quality.")]
            public int MaxIterationCount { get; set; }

            /// <summary>
            /// The agressiveness of the mesh simplification. Higher number equals higher quality, but more expensive to run.
            /// Default value: 7.0
            /// </summary>
            [Tooltip("The agressiveness of the mesh simplification. Higher number equals higher quality, but more expensive to run.")]
            public double Agressiveness { get; set; }

            public Options(double quality)
            {
                this.Quality = quality;
                this.MaxIterationCount = 100;
                this.Agressiveness = 7;
            }
        }

        public struct Result : IDisposable
        {
            internal NativeList<Vertex> Vertices;
            internal NativeList<Triangle> Triangles;

            public void Dispose()
            {
                this.Vertices.Dispose();
                this.Triangles.Dispose();
            }

            public NativeArray<float3> GetVertices(Allocator allocator)
            {
                var vertexCount = this.Vertices.Length;
                var vertices = new NativeArray<float3>(vertexCount, allocator);
                var vertArr = this.Vertices.AsArray();
                for (var i = 0; i < vertexCount; i++)
                {
                    vertices[i] = (float3)vertArr[i].P;
                }

                return vertices;
            }

            public Vector3[] GetVertices()
            {
                var vertexCount = this.Vertices.Length;
                var vertices = new Vector3[vertexCount];
                var vertArr = this.Vertices.AsArray();
                for (var i = 0; i < vertexCount; i++)
                {
                    vertices[i] = (float3)vertArr[i].P;
                }

                return vertices;
            }

            public NativeArray<int> GetIndices(Allocator allocator)
            {
                var triangles = this.Triangles.AsArray();
                var triangleCount = this.Triangles.Length;

                var indices = new NativeArray<int>(triangleCount * 3, allocator);

                for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
                {
                    var triangle = triangles[triangleIndex];
                    var offset = triangleIndex * 3;
                    indices[offset] = triangle.V.x;
                    indices[offset + 1] = triangle.V.y;
                    indices[offset + 2] = triangle.V.z;
                }

                return indices;
            }

            public int[] GetIndices()
            {
                var triangles = this.Triangles.AsArray();
                var triangleCount = this.Triangles.Length;

                var indices = new int[triangleCount * 3];

                for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
                {
                    var triangle = triangles[triangleIndex];
                    var offset = triangleIndex * 3;
                    indices[offset] = triangle.V.x;
                    indices[offset + 1] = triangle.V.y;
                    indices[offset + 2] = triangle.V.z;
                }

                return indices;
            }

            public Mesh GetMesh(bool calculateBounds, bool calculateNormals)
            {
                var vertices = this.GetVertices();
                var indices = this.GetIndices();

                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);

                if (calculateNormals)
                {
                    mesh.RecalculateNormals();
                }

                if (calculateBounds)
                {
                    mesh.RecalculateBounds();
                }

                return mesh;
            }
        }

        internal struct Vertex : IEquatable<Vertex>
        {
            public int Index;
            public double3 P;
            public int TStart;
            public int Tcount;
            public SymmetricMatrix Q;
            public bool BorderEdge;
            public bool UVSeamEdge;
            public bool UVFoldoverEdge;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex(int index, double3 p)
            {
                this.Index = index;
                this.P = p;
                this.TStart = 0;
                this.Tcount = 0;
                this.Q = new SymmetricMatrix();
                this.BorderEdge = true;
                this.UVSeamEdge = false;
                this.UVFoldoverEdge = false;
            }

            public override int GetHashCode()
            {
                return this.Index;
            }

            public bool Equals(Vertex other)
            {
                return this.Index == other.Index;
            }
        }

        internal struct Triangle : IEquatable<Triangle>
        {
            private const double Quantize = 1000000.0;

            public int Index;

            public int3 V;
            public int3 Va;

            public bool Deleted;
            public bool Dirty;
            public float3 N;

            private int err0;
            private int err1;
            private int err2;
            private int err3;

            public double Err0
            {
                get => this.err0 / Quantize;
                set => this.err0 = (int)(value * Quantize);
            }

            public double Err1
            {
                get => this.err1 / Quantize;
                set => this.err1 = (int)(value * Quantize);
            }

            public double Err2
            {
                get => this.err2 / Quantize;
                set => this.err2 = (int)(value * Quantize);
            }

            public double Err3
            {
                get => this.err3 / Quantize;
                set => this.err3 = (int)(value * Quantize);
            }



            public int this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.V[index];
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => this.V[index] = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Triangle(int index, int v0, int v1, int v2)
            {
                this.Index = index;

                this.V = new int3(v0, v1, v2);
                this.Va = this.V;

                this.err0 = this.err1 = this.err2 = this.err3 = 0;
                this.Deleted = this.Dirty = false;
                this.N = new float3();
            }

            public int3 GetAttributeIndices()
            {
                return this.Va;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetAttributeIndex(int index, int value)
            {
                this.Va[index] = value;
            }

            public double3 GetErrors()
            {
                return new double3(this.Err0, this.Err1, this.Err2);
            }

            public override int GetHashCode()
            {
                return this.Index;
            }

            public bool Equals(Triangle other)
            {
                return this.Index == other.Index;
            }
        }

        internal struct Ref
        {
            public int TId;
            public int TVertex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int tid, int tvertex)
            {
                this.TId = tid;
                this.TVertex = tvertex;
            }
        }
    }

    /// <summary> A symmetric matrix. </summary>
    public readonly struct SymmetricMatrix
    {
        /// <summary> The m11 component. </summary>
        public readonly double M0;

        /// <summary> The m12 component. </summary>
        public readonly double M1;

        /// <summary> The m13 component. </summary>
        public readonly double M2;

        /// <summary> The m14 component. </summary>
        public readonly double M3;

        /// <summary> The m22 component. </summary>
        public readonly double M4;

        /// <summary> The m23 component. </summary>
        public readonly double M5;

        /// <summary> The m24 component. </summary>
        public readonly double M6;

        /// <summary> The m33 component. </summary>
        public readonly double M7;

        /// <summary> The m34 component. </summary>
        public readonly double M8;

        /// <summary> The m44 component. </summary>
        public readonly double M9;

        /// <summary> Gets the component value with a specific index. </summary>
        /// <param name="index"> The component index. </param>
        /// <returns> The value. </returns>
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return index switch
                {
                    0 => this.M0,
                    1 => this.M1,
                    2 => this.M2,
                    3 => this.M3,
                    4 => this.M4,
                    5 => this.M5,
                    6 => this.M6,
                    7 => this.M7,
                    8 => this.M8,
                    9 => this.M9,
                    _ => throw new ArgumentOutOfRangeException(nameof(index)),
                };
            }
        }

        /// <summary> Creates a symmetric matrix. </summary>
        /// <param name="m0"> The m11 component. </param>
        /// <param name="m1"> The m12 component. </param>
        /// <param name="m2"> The m13 component. </param>
        /// <param name="m3"> The m14 component. </param>
        /// <param name="m4"> The m22 component. </param>
        /// <param name="m5"> The m23 component. </param>
        /// <param name="m6"> The m24 component. </param>
        /// <param name="m7"> The m33 component. </param>
        /// <param name="m8"> The m34 component. </param>
        /// <param name="m9"> The m44 component. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SymmetricMatrix(double m0, double m1, double m2, double m3, double m4, double m5, double m6, double m7, double m8, double m9)
        {
            this.M0 = m0;
            this.M1 = m1;
            this.M2 = m2;
            this.M3 = m3;
            this.M4 = m4;
            this.M5 = m5;
            this.M6 = m6;
            this.M7 = m7;
            this.M8 = m8;
            this.M9 = m9;
        }

        /// <summary> Creates a symmetric matrix from a plane. </summary>
        /// <param name="a"> The plane x-component. </param>
        /// <param name="b"> The plane y-component </param>
        /// <param name="c"> The plane z-component </param>
        /// <param name="d"> The plane w-component </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SymmetricMatrix(double a, double b, double c, double d)
        {
            this.M0 = a * a;
            this.M1 = a * b;
            this.M2 = a * c;
            this.M3 = a * d;

            this.M4 = b * b;
            this.M5 = b * c;
            this.M6 = b * d;

            this.M7 = c * c;
            this.M8 = c * d;

            this.M9 = d * d;
        }

        /// <summary> Adds two matrixes together. </summary>
        /// <param name="a"> The left hand side. </param>
        /// <param name="b"> The right hand side. </param>
        /// <returns> The resulting matrix. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SymmetricMatrix operator +(SymmetricMatrix a, SymmetricMatrix b)
        {
            return new SymmetricMatrix(
                a.M0 + b.M0, a.M1 + b.M1, a.M2 + b.M2, a.M3 + b.M3, a.M4 + b.M4, a.M5 + b.M5, a.M6 + b.M6, a.M7 + b.M7, a.M8 + b.M8, a.M9 + b.M9);
        }

        /// <summary> Determinant(0, 1, 2, 1, 4, 5, 2, 5, 7) </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant1()
        {
            var det = ((this.M0 * this.M4 * this.M7) + (this.M2 * this.M1 * this.M5) + (this.M1 * this.M5 * this.M2)) -
                (this.M2 * this.M4 * this.M2) -
                (this.M0 * this.M5 * this.M5) -
                (this.M1 * this.M1 * this.M7);

            return det;
        }

        /// <summary> Determinant(1, 2, 3, 4, 5, 6, 5, 7, 8) </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant2()
        {
            var det = ((this.M1 * this.M5 * this.M8) + (this.M3 * this.M4 * this.M7) + (this.M2 * this.M6 * this.M5)) -
                (this.M3 * this.M5 * this.M5) -
                (this.M1 * this.M6 * this.M7) -
                (this.M2 * this.M4 * this.M8);

            return det;
        }

        /// <summary> Determinant(0, 2, 3, 1, 5, 6, 2, 7, 8) </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant3()
        {
            var det = ((this.M0 * this.M5 * this.M8) + (this.M3 * this.M1 * this.M7) + (this.M2 * this.M6 * this.M2)) -
                (this.M3 * this.M5 * this.M2) -
                (this.M0 * this.M6 * this.M7) -
                (this.M2 * this.M1 * this.M8);

            return det;
        }

        /// <summary> Determinant(0, 1, 3, 1, 4, 6, 2, 5, 8) </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant4()
        {
            var det = ((this.M0 * this.M4 * this.M8) + (this.M3 * this.M1 * this.M5) + (this.M1 * this.M6 * this.M2)) -
                (this.M3 * this.M4 * this.M2) -
                (this.M0 * this.M6 * this.M5) -
                (this.M1 * this.M1 * this.M8);

            return det;
        }
    }
}
