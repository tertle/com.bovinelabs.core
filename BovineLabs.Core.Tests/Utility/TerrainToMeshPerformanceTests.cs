#if UNITY_TERRAIN && UNITY_PERFORMANCE_TESTING
namespace BovineLabs.Core.Tests.Utility
{
    using System.IO;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;
    using UnityEngine;

    public class TerrainToMeshPerformanceTests
    {
        private bool _previousBurst;
        private bool _previousCompileSynchronously;
        private bool _previousSafetyChecks;
        private bool _previousForceSafetyChecks;

        [OneTimeSetUp]
        public void SetUp()
        {
            _previousBurst = BurstCompiler.Options.EnableBurstCompilation;
            _previousCompileSynchronously = BurstCompiler.Options.EnableBurstCompileSynchronously;
            _previousSafetyChecks = BurstCompiler.Options.EnableBurstSafetyChecks;
            _previousForceSafetyChecks = BurstCompiler.Options.ForceEnableBurstSafetyChecks;
            BurstCompiler.Options.EnableBurstCompileSynchronously = true;
            BurstCompiler.Options.EnableBurstCompilation = true;
            BurstCompiler.Options.EnableBurstSafetyChecks = false;
            BurstCompiler.Options.ForceEnableBurstSafetyChecks = false;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            BurstCompiler.Options.ForceEnableBurstSafetyChecks = _previousForceSafetyChecks;
            BurstCompiler.Options.EnableBurstSafetyChecks = _previousSafetyChecks;
            BurstCompiler.Options.EnableBurstCompilation = _previousBurst;
            BurstCompiler.Options.EnableBurstCompileSynchronously = _previousCompileSynchronously;
        }

        [TestCase(2, 0, 0)]
        [TestCase(9, 1, 1)]
        [TestCase(17, 2, 2)]
        [TestCase(33, 0, 0)]
        [TestCase(129, 2, 1)]
        [TestCase(257, 1, 0)]
        [TestCase(513, 2, 1)]
        [Performance]
        public void GeometryOnlyPreservesNavMeshInput(int resolution, int profile, int holePattern)
        {
            var heights = new float[resolution, resolution];
            var holes = new bool[resolution - 1, resolution - 1];
            var scale = profile == 0 ? Vector3.one : profile == 1 ? new Vector3(2, 17, 0.75f) : new Vector3(0.25f, 250, 3.5f);
            for (var y = 0; y < resolution; y++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    heights[y, x] = profile == 0 ? 0.125f : profile == 1
                        ? (x + (2f * y)) / (3f * resolution)
                        : ((x * 37 + y * 61 + x * y * 3) % 1024) / 1024f;
                }
            }

            for (var y = 0; y < resolution - 1; y++)
            {
                for (var x = 0; x < resolution - 1; x++)
                {
                    holes[y, x] = holePattern == 0 || (holePattern == 1 && ((x + (3 * y)) % 7 != 0));
                }
            }

            long copiedOutputBytes;
            int triangleIndexCount;
            {
                // The default retains the complete conversion; only the new selection omits UVs and normals.
                using var full = TerrainToMesh.ConvertAsync(resolution, resolution, scale, heights, holes);
                full.WaitForCompletion();
                using var fullVertices = full.GetVerts(Allocator.TempJob);
                using var fullTriangles = full.GetTris(Allocator.TempJob);
                using var geometry = TerrainToMesh.ConvertAsync(resolution, resolution, scale, heights, holes,
                    output: TerrainToMesh.Output.GeometryOnly);
                geometry.WaitForCompletion();
                using var geometryVertices = geometry.GetVerts(Allocator.TempJob);
                using var geometryTriangles = geometry.GetTris(Allocator.TempJob);

                Assert.AreEqual(resolution * resolution, fullVertices.Length);
                Assert.AreEqual(fullVertices.Length, geometryVertices.Length);
                Assert.AreEqual(fullTriangles.Length, geometryTriangles.Length);
                for (var i = 0; i < fullVertices.Length; i++)
                {
                    Assert.AreEqual(math.asuint(fullVertices[i]), math.asuint(geometryVertices[i]), $"Vertex {i} changed float bits.");
                }

                for (var i = 0; i < fullTriangles.Length; i++)
                {
                    Assert.AreEqual(fullTriangles[i], geometryTriangles[i], $"Triangle index {i} changed ordering or hole filtering.");
                }

                if (holePattern == 2)
                {
                    Assert.AreEqual(3, geometryTriangles.Length);
                    Assert.AreEqual(0, geometryTriangles[0]);
                    Assert.AreEqual(0, geometryTriangles[1]);
                    Assert.AreEqual(0, geometryTriangles[2]);
                }

                triangleIndexCount = fullTriangles.Length;
                copiedOutputBytes = ((long)fullVertices.Length * 12) + ((long)fullTriangles.Capacity * 4);
            }

            foreach (var output in new[] { TerrainToMesh.Output.GeometryOnly, TerrainToMesh.Output.FullMesh })
            {
                // Includes input copies, allocation, scheduling/completion, GetVerts/GetTris copies and all native disposal.
                Measure.Method(() =>
                {
                    using var result = TerrainToMesh.ConvertAsync(resolution, resolution, scale, heights, holes, output: output);
                    result.WaitForCompletion();
                    using var vertices = result.GetVerts(Allocator.TempJob);
                    using var triangles = result.GetTris(Allocator.TempJob);
                }).SampleGroup(new SampleGroup($"{output}.ConvertCompleteCopyDispose", SampleUnit.Millisecond))
                    .WarmupCount(3).MeasurementCount(12).IterationsPerMeasurement(1).Run();

                var vertexCount = (long)resolution * resolution;
                var cellCount = (long)(resolution - 1) * (resolution - 1);
                var conversionBytes = (vertexCount * (output == TerrainToMesh.Output.FullMesh ? 36 : 16)) + (cellCount * 25);
                // Payload sizes exclude allocator bookkeeping and managed input arrays; retained triangle capacity includes hole slots.
                Measure.Custom(new SampleGroup($"{output}.ConversionPayloadBytes", SampleUnit.Byte), conversionBytes);
                Measure.Custom(new SampleGroup($"{output}.CopiedOutputCapacityBytes", SampleUnit.Byte), copiedOutputBytes);
                Measure.Custom(new SampleGroup($"{output}.PeakNativePayloadBytes", SampleUnit.Byte), conversionBytes + copiedOutputBytes);
            }

            Measure.Custom(new SampleGroup("Output.Vertices", SampleUnit.Undefined), resolution * resolution);
            Measure.Custom(new SampleGroup("Output.TriangleIndices", SampleUnit.Undefined), triangleIndexCount);
            PerformanceTest.Active.CalculateStatisticalValues();
            File.WriteAllText($"Temp/TerrainToMeshBenchmark-{resolution}-{profile}-{holePattern}.json", JsonUtility.ToJson(PerformanceTest.Active));
        }

        [TestCase(TerrainToMesh.Output.FullMesh, false)]
        [TestCase(TerrainToMesh.Output.GeometryOnly, false)]
        [TestCase(TerrainToMesh.Output.FullMesh, true)]
        [TestCase(TerrainToMesh.Output.GeometryOnly, true)]
        public void NativeAlgorithmPreservesScheduledGeometry(TerrainToMesh.Output output, bool allHoles)
        {
            const int resolution = 9;
            const int vertexCount = resolution * resolution;
            const int cellCount = (resolution - 1) * (resolution - 1);
            var heights = new float[resolution, resolution];
            var holes = new bool[resolution - 1, resolution - 1];
            var scale = new Vector3(0.25f, 250, 3.5f);
            var job = new TerrainToMesh.ComputeTerrainMeshJob
            {
                Width = resolution,
                Height = resolution,
                HeightmapScale = scale,
                GeometryOnly = output == TerrainToMesh.Output.GeometryOnly,
                Heightmap = new NativeArray<float>(vertexCount, Allocator.TempJob),
                Holes = new NativeArray<bool>(cellCount, Allocator.TempJob),
                Positions = new NativeArray<float3>(vertexCount, Allocator.TempJob),
                Uvs = new NativeArray<float2>(output == TerrainToMesh.Output.GeometryOnly ? 0 : vertexCount, Allocator.TempJob),
                Normals = new NativeArray<float3>(output == TerrainToMesh.Output.GeometryOnly ? 0 : vertexCount, Allocator.TempJob),
                Indices = new NativeArray<int>(cellCount * 6, Allocator.TempJob),
            };
            using var nativeProof = new NativeArray<int>(1, Allocator.TempJob);
            try
            {
                for (var i = 0; i < vertexCount; i++)
                {
                    var height = ((i * 37) % 1024) / 1024f;
                    heights[i / resolution, i % resolution] = height;
                    job.Heightmap[i] = height;
                }

                for (var i = 0; i < cellCount; i++)
                {
                    var present = !allHoles && (i % 7 != 0);
                    holes[i / (resolution - 1), i % (resolution - 1)] = present;
                    job.Holes[i] = present;
                }

                new ConversionProbeJob { TerrainJob = job, NativeProof = nativeProof }.Run();
                Assert.AreEqual(1, nativeProof[0], "The existing terrain algorithm must execute native Burst code.");
                using var scheduled = TerrainToMesh.ConvertAsync(resolution, resolution, scale, heights, holes, output: output);
                scheduled.WaitForCompletion();
                using var vertices = scheduled.GetVerts(Allocator.TempJob);
                using var triangles = scheduled.GetTris(Allocator.TempJob);
                for (var i = 0; i < vertexCount; i++)
                {
                    Assert.AreEqual(math.asuint(job.Positions[i]), math.asuint(vertices[i]));
                }

                for (var i = 0; i < cellCount; i++)
                {
                    if (!job.Holes[i])
                    {
                        for (var corner = 0; corner < 6; corner++)
                        {
                            Assert.AreEqual(0, job.Indices[(i * 6) + corner]);
                        }
                    }
                }

                var copied = 0;
                for (var i = 0; i < job.Indices.Length; i += 3)
                {
                    if (job.Indices[i] == 0 || job.Indices[i + 1] == 0 || job.Indices[i + 2] == 0)
                    {
                        continue;
                    }

                    Assert.AreEqual(job.Indices[i], triangles[copied++]);
                    Assert.AreEqual(job.Indices[i + 1], triangles[copied++]);
                    Assert.AreEqual(job.Indices[i + 2], triangles[copied++]);
                }

                if (copied == 0)
                {
                    Assert.AreEqual(3, triangles.Length);
                    Assert.AreEqual(0, triangles[0]);
                    Assert.AreEqual(0, triangles[1]);
                    Assert.AreEqual(0, triangles[2]);
                }
                else
                {
                    Assert.AreEqual(copied, triangles.Length);
                }
            }
            finally
            {
                job.DisposeArrays();
            }
        }

        [BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true)]
        private struct ConversionProbeJob : IJob
        {
            public TerrainToMesh.ComputeTerrainMeshJob TerrainJob;
            public NativeArray<int> NativeProof;

            public void Execute()
            {
                var compiled = 1;
                MarkManaged(ref compiled);
                for (var i = 0; i < TerrainJob.Width * TerrainJob.Height; i++)
                {
                    TerrainJob.Execute(i);
                }

                NativeProof[0] = compiled;
            }

            [BurstDiscard]
            private static void MarkManaged(ref int compiled)
            {
                compiled = 0;
            }
        }
    }
}
#endif
