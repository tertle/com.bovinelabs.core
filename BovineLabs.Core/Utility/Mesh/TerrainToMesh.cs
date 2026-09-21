#if UNITY_TERRAIN
// Contains modified code from com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/Common/TerrainToMesh.cs.
// com.unity.render-pipelines.core copyright © 2020 Unity Technologies ApS.
// Licensed under the Unity Companion License for Unity-dependent projects:
// https://unity.com/legal/licenses/unity-companion-license
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
            private JobHandle _jobHandle;
            private ComputeTerrainMeshJob _job;

            public Result(ComputeTerrainMeshJob job, JobHandle jobHandle)
            {
                _job = job;
                _jobHandle = jobHandle;
            }

            public bool Done => _jobHandle.IsCompleted;

            public JobHandle Dependency => _jobHandle;

            public void Dispose()
            {
                _job.DisposeArrays();
            }

            public Mesh GetMesh()
            {
                Check.Assume(Done);

                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(_job.Positions);
                mesh.SetUVs(0, _job.Uvs);
                mesh.SetNormals(_job.Normals);
                mesh.SetIndices(TriangleIndicesWithoutHoles(Allocator.Temp).AsArray(), MeshTopology.Triangles, 0);

                return mesh;
            }

            public NativeArray<float3> GetVerts(Allocator allocator)
            {
                Check.Assume(Done);

                return new NativeArray<float3>(_job.Positions, allocator);
            }

            public NativeList<int> GetTris(Allocator allocator)
            {
                Check.Assume(Done);

                return TriangleIndicesWithoutHoles(allocator);
            }

            public void WaitForCompletion()
            {
                _jobHandle.Complete();
            }

            private NativeList<int> TriangleIndicesWithoutHoles(Allocator allocator)
            {
                var trianglesWithoutHoles = new NativeList<int>((_job.Width - 1) * (_job.Height - 1) * 6, allocator);
                for (var i = 0; i < _job.Indices.Length; i += 3)
                {
                    var i1 = _job.Indices[i];
                    var i2 = _job.Indices[i + 1];
                    var i3 = _job.Indices[i + 2];

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
                Heightmap.Dispose();
                Holes.Dispose();
                Positions.Dispose();
                Uvs.Dispose();
                Normals.Dispose();
                Indices.Dispose();
            }

            public void Execute(int i)
            {
                var vertexIndex = i;
                var x = i % Width;
                var y = i / Height;

                var v = new float3(x, Heightmap[(y * Width) + x], y);

                Positions[vertexIndex] = v * HeightmapScale;
                Uvs[vertexIndex] = v.xz / new float2(Width, Height);
                Normals[vertexIndex] = CalculateTerrainNormal(Heightmap, x, y, Width, Height, HeightmapScale);

                if (x < Width - 1 && y < Height - 1)
                {
                    var i1 = (y * Width) + x;
                    var i2 = i1 + 1;
                    var i3 = i1 + Width;
                    var i4 = i3 + 1;

                    var faceIndex = x + (y * (Width - 1));

                    if (!Holes[faceIndex])
                    {
                        i1 = i2 = i3 = i4 = 0;
                    }

                    Indices[(6 * faceIndex) + 0] = i1;
                    Indices[(6 * faceIndex) + 1] = i4;
                    Indices[(6 * faceIndex) + 2] = i2;

                    Indices[(6 * faceIndex) + 3] = i1;
                    Indices[(6 * faceIndex) + 4] = i3;
                    Indices[(6 * faceIndex) + 5] = i4;
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
