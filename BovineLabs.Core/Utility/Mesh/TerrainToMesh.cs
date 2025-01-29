// <copyright file="TerrainToMesh.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_TERRAIN
// Modified version of com.unity.rendering.light-transport\Runtime\UnifiedRayTracing\Common\TerrainToMesh.cs
namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.Rendering;

    public static class TerrainToMesh
    {
        public static Result Convert(TerrainData terrainData, Allocator allocator = Allocator.TempJob)
        {
            var request = ConvertAsync(terrainData, allocator);
            request.WaitForCompletion();
            return request;
        }

        public static Result ConvertAsync(TerrainData terrainData, Allocator allocator = Allocator.TempJob)
        {
            var width = terrainData.heightmapTexture.width;
            var height = terrainData.heightmapTexture.height;
            var heightmap = terrainData.GetHeights(0, 0, width, height);
            var holes = terrainData.GetHoles(0, 0, width - 1, height - 1);

            return ConvertAsync(width, height, terrainData.heightmapScale, heightmap, holes, allocator);
        }

        public static Result ConvertAsync(
            int width, int height, Vector3 heightmapScale, float[,] heightmap, bool[,] holes, Allocator allocator = Allocator.TempJob)
        {
            var vertexCount = width * height;
            var job = default(ComputeTerrainMeshJob);
            job.Heightmap = new NativeArray<float>(vertexCount, allocator);
            for (var i = 0; i < vertexCount; ++i)
            {
                job.Heightmap[i] = heightmap[i / width, i % width];
            }

            job.Holes = new NativeArray<bool>((width - 1) * (height - 1), allocator);
            for (var i = 0; i < (width - 1) * (height - 1); ++i)
            {
                job.Holes[i] = holes[i / (width - 1), i % (width - 1)];
            }

            job.Width = width;
            job.Height = height;
            job.HeightmapScale = heightmapScale;

            job.Positions = new NativeArray<float3>(vertexCount, allocator);
            job.Uvs = new NativeArray<float2>(vertexCount, allocator);
            job.Normals = new NativeArray<float3>(vertexCount, allocator);
            job.Indices = new NativeArray<int>((width - 1) * (height - 1) * 6, allocator);

            var jobHandle = job.ScheduleParallel(vertexCount, math.max(width, 128), default);

            return new Result(job, jobHandle);
        }

        public struct Result : IDisposable
        {
            private JobHandle jobHandle;
            private ComputeTerrainMeshJob job;

            public Result(ComputeTerrainMeshJob job, JobHandle jobHandle)
            {
                this.job = job;
                this.jobHandle = jobHandle;
            }

            public bool Done => this.jobHandle.IsCompleted;

            public JobHandle Dependency => this.jobHandle;

            public void Dispose()
            {
                this.job.DisposeArrays();
            }

            public Mesh GetMesh()
            {
                Check.Assume(this.Done);

                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(this.job.Positions);
                mesh.SetUVs(0, this.job.Uvs);
                mesh.SetNormals(this.job.Normals);
                mesh.SetIndices(this.TriangleIndicesWithoutHoles(Allocator.Temp).AsArray(), MeshTopology.Triangles, 0);

                return mesh;
            }

            public NativeArray<float3> GetVerts(Allocator allocator)
            {
                Check.Assume(this.Done);

                return new NativeArray<float3>(this.job.Positions, allocator);
            }

            public NativeList<int> GetTris(Allocator allocator)
            {
                Check.Assume(this.Done);

                return this.TriangleIndicesWithoutHoles(allocator);
            }

            public void WaitForCompletion()
            {
                this.jobHandle.Complete();
            }

            private NativeList<int> TriangleIndicesWithoutHoles(Allocator allocator)
            {
                var trianglesWithoutHoles = new NativeList<int>((this.job.Width - 1) * (this.job.Height - 1) * 6, allocator);
                for (var i = 0; i < this.job.Indices.Length; i += 3)
                {
                    var i1 = this.job.Indices[i];
                    var i2 = this.job.Indices[i + 1];
                    var i3 = this.job.Indices[i + 2];

                    if (i1 != 0 && i2 != 0 && i3 != 0)
                    {
                        trianglesWithoutHoles.Add(i1);
                        trianglesWithoutHoles.Add(i2);
                        trianglesWithoutHoles.Add(i3);
                    }
                }

                if (trianglesWithoutHoles.Length == 0)
                {
                    trianglesWithoutHoles.Add(0);
                    trianglesWithoutHoles.Add(0);
                    trianglesWithoutHoles.Add(0);
                }

                return trianglesWithoutHoles;
            }
        }

        [BurstCompile]
        public struct ComputeTerrainMeshJob : IJobFor
        {
            [ReadOnly]
            public NativeArray<float> Heightmap;

            [ReadOnly]
            public NativeArray<bool> Holes;

            public int Width;
            public int Height;
            public float3 HeightmapScale;

            public NativeArray<float3> Positions;
            public NativeArray<float2> Uvs;
            public NativeArray<float3> Normals;

            [NativeDisableParallelForRestriction]
            public NativeArray<int> Indices;

            public void DisposeArrays()
            {
                this.Heightmap.Dispose();
                this.Holes.Dispose();
                this.Positions.Dispose();
                this.Uvs.Dispose();
                this.Normals.Dispose();
                this.Indices.Dispose();
            }

            public void Execute(int i)
            {
                var vertexIndex = i;
                var x = i % this.Width;
                var y = i / this.Height;

                var v = new float3(x, this.Heightmap[(y * this.Width) + x], y);

                this.Positions[vertexIndex] = v * this.HeightmapScale;
                this.Uvs[vertexIndex] = v.xz / new float2(this.Width, this.Height);
                this.Normals[vertexIndex] = CalculateTerrainNormal(this.Heightmap, x, y, this.Width, this.Height, this.HeightmapScale);

                if (x < this.Width - 1 && y < this.Height - 1)
                {
                    var i1 = (y * this.Width) + x;
                    var i2 = i1 + 1;
                    var i3 = i1 + this.Width;
                    var i4 = i3 + 1;

                    var faceIndex = x + (y * (this.Width - 1));

                    if (!this.Holes[faceIndex])
                    {
                        i1 = i2 = i3 = i4 = 0;
                    }

                    this.Indices[(6 * faceIndex) + 0] = i1;
                    this.Indices[(6 * faceIndex) + 1] = i4;
                    this.Indices[(6 * faceIndex) + 2] = i2;

                    this.Indices[(6 * faceIndex) + 3] = i1;
                    this.Indices[(6 * faceIndex) + 4] = i3;
                    this.Indices[(6 * faceIndex) + 5] = i4;
                }
            }

            private static float3 CalculateTerrainNormal(NativeArray<float> heightmap, int x, int y, int width, int height, float3 scale)
            {
                var dX = SampleHeight(x - 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
                dX += SampleHeight(x - 1, y, width, height, heightmap, scale.y) * -2.0F;
                dX += SampleHeight(x - 1, y + 1, width, height, heightmap, scale.y) * -1.0F;
                dX += SampleHeight(x + 1, y - 1, width, height, heightmap, scale.y) * 1.0F;
                dX += SampleHeight(x + 1, y, width, height, heightmap, scale.y) * 2.0F;
                dX += SampleHeight(x + 1, y + 1, width, height, heightmap, scale.y) * 1.0F;

                dX /= scale.x;

                var dY = SampleHeight(x - 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
                dY += SampleHeight(x, y - 1, width, height, heightmap, scale.y) * -2.0F;
                dY += SampleHeight(x + 1, y - 1, width, height, heightmap, scale.y) * -1.0F;
                dY += SampleHeight(x - 1, y + 1, width, height, heightmap, scale.y) * 1.0F;
                dY += SampleHeight(x, y + 1, width, height, heightmap, scale.y) * 2.0F;
                dY += SampleHeight(x + 1, y + 1, width, height, heightmap, scale.y) * 1.0F;
                dY /= scale.z;

                // Cross Product of components of gradient reduces to
                return math.normalize(new float3(-dX, 8, -dY));
            }

            private static float SampleHeight(int x, int y, int width, int height, NativeArray<float> heightmap, float scale)
            {
                x = math.clamp(x, 0, width - 1);
                y = math.clamp(y, 0, height - 1);

                return heightmap[x + (y * width)] * scale;
            }
        }
    }
}
#endif
