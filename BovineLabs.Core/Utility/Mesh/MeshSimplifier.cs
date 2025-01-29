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
                    triangles.ElementAt(i).dirty = false;
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
                    if (!triangles[i].deleted)
                    {
                        if (dst != i)
                        {
                            triangles[dst] = triangles[i];
                            triangles.ElementAt(dst).index = dst;
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
                    v.borderEdge = false;
                    v.uvSeamEdge = false;
                    v.uvFoldoverEdge = false;
                }

                for (var i = 0; i < vertexCount; i++)
                {
                    var tstart = vertices[i].tstart;
                    var tcount = vertices[i].tcount;
                    vcount.Clear();
                    vids.Clear();
                    vsize = 0;

                    int id;
                    for (var j = 0; j < tcount; j++)
                    {
                        var tid = refArr[tstart + j].tid;
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
                            vertices.ElementAt(id).borderEdge = true;
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
                    vertices.ElementAt(i).q = default;
                }

                for (var i = 0; i < triangleCount; i++)
                {
                    var v = triangles[i].v;

                    var p0 = vertices[v.x].p;
                    var p1 = vertices[v.y].p;
                    var p2 = vertices[v.z].p;
                    var p10 = p1 - p0;
                    var p20 = p2 - p0;
                    var n = math.cross(p10, p20);
                    n = math.normalizesafe(n);
                    triangles.ElementAt(i).n = n;

                    var sm = new SymmetricMatrix(n.x, n.y, n.z, -math.dot(n, p0));
                    vertices.ElementAt(v.x).q += sm;
                    vertices.ElementAt(v.y).q += sm;
                    vertices.ElementAt(v.z).q += sm;
                }

                for (var i = 0; i < triangleCount; i++)
                {
                    // Calc Edge Error
                    // var triangle = triangles[i];
                    ref var triangle = ref triangles.ElementAt(i);

                    triangle.err0 = CalculateError(ref vertices.ElementAt(triangle.v.x), ref vertices.ElementAt(triangle.v.y), out _);
                    triangle.err1 = CalculateError(ref vertices.ElementAt(triangle.v.y), ref vertices.ElementAt(triangle.v.z), out _);
                    triangle.err2 = CalculateError(ref vertices.ElementAt(triangle.v.z), ref vertices.ElementAt(triangle.v.x), out _);
                    triangle.err3 = math.cmin(new double3(triangle.err0, triangle.err1, triangle.err2));
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
                if (triangles[tid].dirty || triangles[tid].deleted || triangles[tid].err3 > threshold)
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
                    if (vertices[i0].borderEdge != vertices[i1].borderEdge)
                    {
                        continue;
                    }

                    // Seam check
                    if (vertices[i0].uvSeamEdge != vertices[i1].uvSeamEdge)
                    {
                        continue;
                    }

                    // Foldover check
                    if (vertices[i0].uvFoldoverEdge != vertices[i1].uvFoldoverEdge)
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
                    deleted0.Resize(vertices[i0].tcount, NativeArrayOptions.UninitializedMemory); // normals temporarily
                    deleted1.Resize(vertices[i1].tcount, NativeArrayOptions.UninitializedMemory); // normals temporarily

                    // Don't remove if flipped
                    if (Flipped(ref result, ref refs, ref p, i0, i1, ref vertices.ElementAt(i0), deleted0.AsArray()))
                    {
                        continue;
                    }

                    if (Flipped(ref result, ref refs, ref p, i1, i0, ref vertices.ElementAt(i1), deleted1.AsArray()))
                    {
                        continue;
                    }

                    // Calculate the barycentric coordinates within the triangle
                    var nextNextEdgeIndex = (edgeIndex + 2) % 3;
                    var i2 = triangles[tid][nextNextEdgeIndex];
                    CalculateBarycentricCoords(ref p, ref vertices.ElementAt(i0).p, ref vertices.ElementAt(i1).p, ref vertices.ElementAt(i2).p, out _);

                    // Not flipped, so remove edge
                    vertices.ElementAt(i0).p = p;
                    vertices.ElementAt(i0).q += vertices[i1].q;

                    // Interpolate the vertex attributes
                    var ia0 = attributeIndexArr[edgeIndex];

                    if (vertices[i0].uvSeamEdge)
                    {
                        ia0 = -1;
                    }

                    var tstart = refs.Length;

                    UpdateTriangles(ref result, ref refs, i0, ia0, ref vertices.ElementAt(i0), deleted0, ref deletedTris);
                    UpdateTriangles(ref result, ref refs, i0, ia0, ref vertices.ElementAt(i1), deleted1, ref deletedTris);

                    var tcount = refs.Length - tstart;
                    if (tcount <= vertices[i0].tcount)
                    {
                        // save ram
                        if (tcount > 0)
                        {
                            var refsArr = refs.AsArray();

                            // TODO does this need a memmove?
                            var src = refsArr.GetSubArray(tstart, tcount);
                            var dst = refsArr.GetSubArray(vertices[i0].tstart, tcount);
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
                        vertices.ElementAt(i0).tstart = tstart;
                    }

                    vertices.ElementAt(i0).tcount = tcount;
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
                vertices.ElementAt(i).tcount = 0;
            }

            var triangles = result.Triangles.AsArray();
            var triangleCount = result.Triangles.Length;
            for (var i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                if (!triangle.deleted)
                {
                    if (triangle.va.x != triangle.v.x)
                    {
                        var iDest = triangle.va.x;
                        var iSrc = triangle.v.x;
                        vertices.ElementAt(iDest).p = vertices[iSrc].p;
                        triangle.v.x = triangle.va.x;
                    }

                    if (triangle.va.y != triangle.v.y)
                    {
                        var iDest = triangle.va.y;
                        var iSrc = triangle.v.y;
                        vertices.ElementAt(iDest).p = vertices[iSrc].p;
                        triangle.v.y = triangle.va.y;
                    }

                    if (triangle.va.z != triangle.v.z)
                    {
                        var iDest = triangle.va.z;
                        var iSrc = triangle.v.z;
                        vertices.ElementAt(iDest).p = vertices[iSrc].p;
                        triangle.v.z = triangle.va.z;
                    }

                    var newTriangleIndex = dst++;
                    triangles[newTriangleIndex] = triangle;
                    triangles.ElementAt(newTriangleIndex).index = newTriangleIndex;

                    vertices.ElementAt(triangle.v.x).tcount = 1;
                    vertices.ElementAt(triangle.v.y).tcount = 1;
                    vertices.ElementAt(triangle.v.z).tcount = 1;
                }
            }

            triangleCount = dst;

            result.Triangles.Resize(triangleCount, NativeArrayOptions.UninitializedMemory);
            triangles = result.Triangles.AsArray();

            dst = 0;
            for (var i = 0; i < vertexCount; i++)
            {
                var vert = vertices[i];
                if (vert.tcount > 0)
                {
                    vertices.ElementAt(i).tstart = dst;

                    if (dst != i)
                    {
                        vertices.ElementAt(dst).index = dst;
                        vertices.ElementAt(dst).p = vert.p;
                    }

                    ++dst;
                }
            }

            for (var i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                triangle.v.x = vertices[triangle.v.x].tstart;
                triangle.v.y = vertices[triangle.v.y].tstart;
                triangle.v.z = vertices[triangle.v.z].tstart;
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
                v.tstart = 0;
                v.tcount = 0;
            }

            for (var i = 0; i < triangleCount; i++)
            {
                ++vertices.ElementAt(triangles[i].v.x).tcount;
                ++vertices.ElementAt(triangles[i].v.y).tcount;
                ++vertices.ElementAt(triangles[i].v.z).tcount;
            }

            var tstart = 0;
            for (var i = 0; i < vertexCount; i++)
            {
                ref var v = ref vertices.ElementAt(i);
                v.tstart = tstart;
                tstart += v.tcount;
                v.tcount = 0;
            }

            // Write References
            refs.ResizeUninitialized(tstart);
            var refArr = refs.AsArray();
            for (var i = 0; i < triangleCount; i++)
            {
                var v = triangles[i].v;

                ref var v0 = ref vertices.ElementAt(v.x);
                ref var v1 = ref vertices.ElementAt(v.y);
                ref var v2 = ref vertices.ElementAt(v.z);

                var start0 = v0.tstart;
                var count0 = v0.tcount++;
                var start1 = v1.tstart;
                var count1 = v1.tcount++;
                var start2 = v2.tstart;
                var count2 = v2.tcount++;

                refArr.ElementAt(start0 + count0).Set(i, 0);
                refArr.ElementAt(start1 + count1).Set(i, 1);
                refArr.ElementAt(start2 + count2).Set(i, 2);
            }
        }

        private static double CalculateError(ref Vertex vert0, ref Vertex vert1, out double3 result)
        {
            // compute interpolated vertex
            var q = vert0.q + vert1.q;
            var borderEdge = vert0.borderEdge && vert1.borderEdge;
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
                var p1 = vert0.p;
                var p2 = vert1.p;
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
            return (q.m0 * x * x) +
                (2 * q.m1 * x * y) +
                (2 * q.m2 * x * z) +
                (2 * q.m3 * x) +
                (q.m4 * y * y) +
                (2 * q.m5 * y * z) +
                (2 * q.m6 * y) +
                (q.m7 * z * z) +
                (2 * q.m8 * z) +
                q.m9;
        }

        private static bool Flipped(ref Result result, ref NativeList<Ref> refs, ref double3 p, int i0, int i1, ref Vertex v0, NativeArray<bool> deleted)
        {
            var tcount = v0.tcount;
            var refsArr = refs.AsArray();
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();
            for (var k = 0; k < tcount; k++)
            {
                var r = refsArr[v0.tstart + k];
                if (triangles[r.tid].deleted)
                {
                    continue;
                }

                var s = r.tvertex;
                var id1 = triangles[r.tid][(s + 1) % 3];
                var id2 = triangles[r.tid][(s + 2) % 3];
                if (id1 == i1 || id2 == i1)
                {
                    deleted[k] = true;
                    continue;
                }

                var d1 = vertices[id1].p - p;
                d1 = math.normalizesafe(d1);
                var d2 = vertices[id2].p - p;
                d2 = math.normalizesafe(d2);
                var dot = math.dot(d1, d2);
                if (Math.Abs(dot) > 0.999)
                {
                    return true;
                }

                var n = math.cross(d1, d2);
                n = math.normalizesafe(n);
                deleted[k] = false;
                dot = math.dot(n, triangles[r.tid].n);
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
            var tcount = v.tcount;
            var triangles = result.Triangles.AsArray();
            var vertices = result.Vertices.AsArray();
            for (var k = 0; k < tcount; k++)
            {
                var r = refs[v.tstart + k];
                var tid = r.tid;
                var t = triangles[tid];
                if (t.deleted)
                {
                    continue;
                }

                if (deleted[k])
                {
                    triangles.ElementAt(tid).deleted = true;
                    ++deletedTriangles;
                    continue;
                }

                t[r.tvertex] = i0;
                if (ia0 != -1)
                {
                    t.SetAttributeIndex(r.tvertex, ia0);
                }

                t.dirty = true;
                t.err0 = CalculateError(ref vertices.ElementAt(t.v.x), ref vertices.ElementAt(t.v.y), out _);
                t.err1 = CalculateError(ref vertices.ElementAt(t.v.y), ref vertices.ElementAt(t.v.z), out _);
                t.err2 = CalculateError(ref vertices.ElementAt(t.v.z), ref vertices.ElementAt(t.v.x), out _);
                t.err3 = math.cmin(new double3(t.err0, t.err1, t.err2));
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
                    vertices[i] = (float3)vertArr[i].p;
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
                    vertices[i] = (float3)vertArr[i].p;
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
                    indices[offset] = triangle.v.x;
                    indices[offset + 1] = triangle.v.y;
                    indices[offset + 2] = triangle.v.z;
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
                    indices[offset] = triangle.v.x;
                    indices[offset + 1] = triangle.v.y;
                    indices[offset + 2] = triangle.v.z;
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
            public int index;
            public double3 p;
            public int tstart;
            public int tcount;
            public SymmetricMatrix q;
            public bool borderEdge;
            public bool uvSeamEdge;
            public bool uvFoldoverEdge;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Vertex(int index, double3 p)
            {
                this.index = index;
                this.p = p;
                this.tstart = 0;
                this.tcount = 0;
                this.q = new SymmetricMatrix();
                this.borderEdge = true;
                this.uvSeamEdge = false;
                this.uvFoldoverEdge = false;
            }

            public override int GetHashCode()
            {
                return this.index;
            }

            public bool Equals(Vertex other)
            {
                return this.index == other.index;
            }
        }

        internal struct Triangle : IEquatable<Triangle>
        {
            public int index;

            public int3 v;
            public int3 va;

            public double err0;
            public double err1;
            public double err2;
            public double err3;

            public bool deleted;
            public bool dirty;
            public double3 n;

            public int this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.v[index];
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set => this.v[index] = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Triangle(int index, int v0, int v1, int v2)
            {
                this.index = index;

                this.v = new int3(v0, v1, v2);
                this.va = this.v;

                this.err0 = this.err1 = this.err2 = this.err3 = 0;
                this.deleted = this.dirty = false;
                this.n = new double3();
            }

            public int3 GetAttributeIndices()
            {
                return this.va;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetAttributeIndex(int index, int value)
            {
                this.va[index] = value;
            }

            public double3 GetErrors()
            {
                return new double3(this.err0, this.err1, this.err2);
            }

            public override int GetHashCode()
            {
                return this.index;
            }

            public bool Equals(Triangle other)
            {
                return this.index == other.index;
            }
        }

        internal struct Ref
        {
            public int tid;
            public int tvertex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set(int tid, int tvertex)
            {
                this.tid = tid;
                this.tvertex = tvertex;
            }
        }
    }

    // TODO?
    /// <summary>
    /// A symmetric matrix.
    /// </summary>
    public struct SymmetricMatrix
    {
#region Fields
        /// <summary>
        /// The m11 component.
        /// </summary>
        public double m0;
        /// <summary>
        /// The m12 component.
        /// </summary>
        public double m1;
        /// <summary>
        /// The m13 component.
        /// </summary>
        public double m2;
        /// <summary>
        /// The m14 component.
        /// </summary>
        public double m3;
        /// <summary>
        /// The m22 component.
        /// </summary>
        public double m4;
        /// <summary>
        /// The m23 component.
        /// </summary>
        public double m5;
        /// <summary>
        /// The m24 component.
        /// </summary>
        public double m6;
        /// <summary>
        /// The m33 component.
        /// </summary>
        public double m7;
        /// <summary>
        /// The m34 component.
        /// </summary>
        public double m8;
        /// <summary>
        /// The m44 component.
        /// </summary>
        public double m9;
#endregion

#region Properties
        /// <summary>
        /// Gets the component value with a specific index.
        /// </summary>
        /// <param name="index"> The component index. </param>
        /// <returns> The value. </returns>
        public double this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0:
                        return this.m0;
                    case 1:
                        return this.m1;
                    case 2:
                        return this.m2;
                    case 3:
                        return this.m3;
                    case 4:
                        return this.m4;
                    case 5:
                        return this.m5;
                    case 6:
                        return this.m6;
                    case 7:
                        return this.m7;
                    case 8:
                        return this.m8;
                    case 9:
                        return this.m9;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }
#endregion

#region Constructor
        /// <summary>
        /// Creates a symmetric matrix with a value in each component.
        /// </summary>
        /// <param name="c"> The component value. </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SymmetricMatrix(double c)
        {
            this.m0 = c;
            this.m1 = c;
            this.m2 = c;
            this.m3 = c;
            this.m4 = c;
            this.m5 = c;
            this.m6 = c;
            this.m7 = c;
            this.m8 = c;
            this.m9 = c;
        }

        /// <summary>
        /// Creates a symmetric matrix.
        /// </summary>
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
            this.m0 = m0;
            this.m1 = m1;
            this.m2 = m2;
            this.m3 = m3;
            this.m4 = m4;
            this.m5 = m5;
            this.m6 = m6;
            this.m7 = m7;
            this.m8 = m8;
            this.m9 = m9;
        }

        /// <summary>
        /// Creates a symmetric matrix from a plane.
        /// </summary>
        /// <param name="a"> The plane x-component. </param>
        /// <param name="b"> The plane y-component </param>
        /// <param name="c"> The plane z-component </param>
        /// <param name="d"> The plane w-component </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SymmetricMatrix(double a, double b, double c, double d)
        {
            this.m0 = a * a;
            this.m1 = a * b;
            this.m2 = a * c;
            this.m3 = a * d;

            this.m4 = b * b;
            this.m5 = b * c;
            this.m6 = b * d;

            this.m7 = c * c;
            this.m8 = c * d;

            this.m9 = d * d;
        }
#endregion

#region Operators
        /// <summary>
        /// Adds two matrixes together.
        /// </summary>
        /// <param name="a"> The left hand side. </param>
        /// <param name="b"> The right hand side. </param>
        /// <returns> The resulting matrix. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SymmetricMatrix operator +(SymmetricMatrix a, SymmetricMatrix b)
        {
            return new SymmetricMatrix(a.m0 + b.m0, a.m1 + b.m1, a.m2 + b.m2, a.m3 + b.m3, a.m4 + b.m4, a.m5 + b.m5, a.m6 + b.m6, a.m7 + b.m7, a.m8 + b.m8,
                a.m9 + b.m9);
        }
#endregion

#region Internal Methods
        /// <summary>
        /// Determinant(0, 1, 2, 1, 4, 5, 2, 5, 7)
        /// </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant1()
        {
            var det = ((this.m0 * this.m4 * this.m7) + (this.m2 * this.m1 * this.m5) + (this.m1 * this.m5 * this.m2)) -
                (this.m2 * this.m4 * this.m2) -
                (this.m0 * this.m5 * this.m5) -
                (this.m1 * this.m1 * this.m7);

            return det;
        }

        /// <summary>
        /// Determinant(1, 2, 3, 4, 5, 6, 5, 7, 8)
        /// </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant2()
        {
            var det = ((this.m1 * this.m5 * this.m8) + (this.m3 * this.m4 * this.m7) + (this.m2 * this.m6 * this.m5)) -
                (this.m3 * this.m5 * this.m5) -
                (this.m1 * this.m6 * this.m7) -
                (this.m2 * this.m4 * this.m8);

            return det;
        }

        /// <summary>
        /// Determinant(0, 2, 3, 1, 5, 6, 2, 7, 8)
        /// </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant3()
        {
            var det = ((this.m0 * this.m5 * this.m8) + (this.m3 * this.m1 * this.m7) + (this.m2 * this.m6 * this.m2)) -
                (this.m3 * this.m5 * this.m2) -
                (this.m0 * this.m6 * this.m7) -
                (this.m2 * this.m1 * this.m8);

            return det;
        }

        /// <summary>
        /// Determinant(0, 1, 3, 1, 4, 6, 2, 5, 8)
        /// </summary>
        /// <returns> </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double Determinant4()
        {
            var det = ((this.m0 * this.m4 * this.m8) + (this.m3 * this.m1 * this.m5) + (this.m1 * this.m6 * this.m2)) -
                (this.m3 * this.m4 * this.m2) -
                (this.m0 * this.m6 * this.m5) -
                (this.m1 * this.m1 * this.m8);

            return det;
        }
#endregion

#region Public Methods
        /// <summary>
        /// Computes the determinant of this matrix.
        /// </summary>
        /// <param name="a11"> The a11 index. </param>
        /// <param name="a12"> The a12 index. </param>
        /// <param name="a13"> The a13 index. </param>
        /// <param name="a21"> The a21 index. </param>
        /// <param name="a22"> The a22 index. </param>
        /// <param name="a23"> The a23 index. </param>
        /// <param name="a31"> The a31 index. </param>
        /// <param name="a32"> The a32 index. </param>
        /// <param name="a33"> The a33 index. </param>
        /// <returns> The determinant value. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant(int a11, int a12, int a13, int a21, int a22, int a23, int a31, int a32, int a33)
        {
            var det = ((this[a11] * this[a22] * this[a33]) + (this[a13] * this[a21] * this[a32]) + (this[a12] * this[a23] * this[a31])) -
                (this[a13] * this[a22] * this[a31]) -
                (this[a11] * this[a23] * this[a32]) -
                (this[a12] * this[a21] * this[a33]);

            return det;
        }
#endregion
    }
}
