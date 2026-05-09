// <copyright file="SpatialMapPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PerformanceTests.Spatial
{
    using BovineLabs.Core.Spatial;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using Unity.Mathematics;
    using Unity.PerformanceTesting;

    public class SpatialMapPerformanceTests
    {
        private const int WorldSize = 4096;
        private const float QuantizeStep = 16f;
        private const float QueryRadius = 20f;
        private const int QueryCount = 256;
        private const int WarmupCount = 5;
        private const int MeasurementCount = 10;
        private const uint Seed = 1234;

        public static object[] Cases =
        {
            new object[] { 4096, Distribution.Uniform },
            new object[] { 4096, Distribution.Clustered },
            new object[] { 16384, Distribution.Uniform },
            new object[] { 16384, Distribution.Clustered },
        };

        [TestCaseSource(nameof(Cases))]
        [Performance]
        public void SpatialMapBuild(int count, Distribution distribution)
        {
            var positions = new NativeArray<TestSpatialPosition>(count, Allocator.Persistent);
            var map = new SpatialMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                PopulatePositions(positions, distribution);

                Measure.Method(() => map.Build(positions, default).Complete())
                    .WarmupCount(WarmupCount)
                    .MeasurementCount(MeasurementCount)
                    .Run();
            }
            finally
            {
                map.Dispose();
                positions.Dispose();
            }
        }

        [TestCaseSource(nameof(Cases))]
        [Performance]
        public void HexSpatialMapBuild(int count, Distribution distribution)
        {
            var positions = new NativeArray<TestSpatialPosition>(count, Allocator.Persistent);
            var map = new SpatialHexMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                PopulatePositions(positions, distribution);

                Measure.Method(() => map.Build(positions, default).Complete())
                    .WarmupCount(WarmupCount)
                    .MeasurementCount(MeasurementCount)
                    .Run();
            }
            finally
            {
                map.Dispose();
                positions.Dispose();
            }
        }

        [TestCaseSource(nameof(Cases))]
        [Performance]
        public void SpatialMapQuery(int count, Distribution distribution)
        {
            var positions = new NativeArray<TestSpatialPosition>(count, Allocator.Persistent);
            var queries = new NativeArray<float2>(QueryCount, Allocator.Persistent);
            var result = new NativeReference<int>(Allocator.Persistent);
            var map = new SpatialMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                PopulatePositions(positions, distribution);
                PopulateQueries(queries, distribution);

                Measure.Method(() =>
                    {
                        new SpatialMapQueryJob
                        {
                            Queries = queries,
                            Positions = positions,
                            Map = map.AsReadOnly(),
                            Radius = QueryRadius,
                            Result = result,
                        }.Run();
                    })
                    .SetUp(() => map.Build(positions, default).Complete())
                    .WarmupCount(WarmupCount)
                    .MeasurementCount(MeasurementCount)
                    .Run();
            }
            finally
            {
                map.Dispose();
                result.Dispose();
                queries.Dispose();
                positions.Dispose();
            }
        }

        [TestCaseSource(nameof(Cases))]
        [Performance]
        public void HexSpatialMapQuery(int count, Distribution distribution)
        {
            var positions = new NativeArray<TestSpatialPosition>(count, Allocator.Persistent);
            var queries = new NativeArray<float2>(QueryCount, Allocator.Persistent);
            var result = new NativeReference<int>(Allocator.Persistent);
            var map = new SpatialHexMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                PopulatePositions(positions, distribution);
                PopulateQueries(queries, distribution);

                Measure.Method(() =>
                    {
                        new HexSpatialMapQueryJob
                        {
                            Queries = queries,
                            Positions = positions,
                            Map = map.AsReadOnly(),
                            Radius = QueryRadius,
                            Result = result,
                        }.Run();
                    })
                    .SetUp(() => map.Build(positions, default).Complete())
                    .WarmupCount(WarmupCount)
                    .MeasurementCount(MeasurementCount)
                    .Run();
            }
            finally
            {
                map.Dispose();
                result.Dispose();
                queries.Dispose();
                positions.Dispose();
            }
        }

        private static void PopulatePositions(NativeArray<TestSpatialPosition> positions, Distribution distribution)
        {
            var random = Random.CreateFromIndex(Seed + (uint)distribution);
            var half = (WorldSize * 0.5f) - 1f;
            var clusterA = new float2(-900f, -900f);
            var clusterB = new float2(850f, -650f);
            var clusterC = new float2(-700f, 800f);
            var clusterD = new float2(950f, 950f);

            for (var i = 0; i < positions.Length; i++)
            {
                float2 position;

                if (distribution == Distribution.Uniform)
                {
                    position = random.NextFloat2(new float2(-half), new float2(half));
                }
                else
                {
                    var cluster = i & 3;
                    var center = cluster switch
                    {
                        0 => clusterA,
                        1 => clusterB,
                        2 => clusterC,
                        _ => clusterD,
                    };

                    position = center + random.NextFloat2Direction() * random.NextFloat(0f, 180f);
                    position = math.clamp(position, new float2(-half), new float2(half));
                }

                positions[i] = new TestSpatialPosition { Position = position };
            }
        }

        private static void PopulateQueries(NativeArray<float2> queries, Distribution distribution)
        {
            var random = Random.CreateFromIndex(Seed + 97u + (uint)distribution);
            var half = (WorldSize * 0.5f) - QueryRadius - QuantizeStep;
            var clusterA = new float2(-900f, -900f);
            var clusterB = new float2(850f, -650f);
            var clusterC = new float2(-700f, 800f);
            var clusterD = new float2(950f, 950f);

            for (var i = 0; i < queries.Length; i++)
            {
                float2 query;

                if (distribution == Distribution.Uniform)
                {
                    query = random.NextFloat2(new float2(-half), new float2(half));
                }
                else
                {
                    var cluster = i & 3;
                    var center = cluster switch
                    {
                        0 => clusterA,
                        1 => clusterB,
                        2 => clusterC,
                        _ => clusterD,
                    };

                    query = center + random.NextFloat2Direction() * random.NextFloat(0f, 220f);
                    query = math.clamp(query, new float2(-half), new float2(half));
                }

                queries[i] = query;
            }
        }

        public enum Distribution : uint
        {
            Uniform,
            Clustered,
        }

        private struct TestSpatialPosition : ISpatialPosition
        {
            public float2 Position { get; set; }
        }

        [BurstCompile]
        private struct SpatialMapQueryJob : IJob
        {
            [ReadOnly]
            public NativeArray<float2> Queries;

            [ReadOnly]
            public NativeArray<TestSpatialPosition> Positions;

            [ReadOnly]
            public SpatialMap.ReadOnly Map;

            public float Radius;

            public NativeReference<int> Result;

            public void Execute()
            {
                var radiusSq = this.Radius * this.Radius;
                var total = 0;

                for (var i = 0; i < this.Queries.Length; i++)
                {
                    var query = this.Queries[i];
                    var min = this.Map.Quantized(query - this.Radius);
                    var max = this.Map.Quantized(query + this.Radius);

                    for (var y = min.y; y <= max.y; y++)
                    {
                        for (var x = min.x; x <= max.x; x++)
                        {
                            var cell = new int2(x, y);
                            var cellMinDistanceSq = CalculateSquareCellMinDistanceSq(query, cell);
                            if (cellMinDistanceSq > radiusSq)
                            {
                                continue;
                            }

                            var hash = this.Map.Hash(cell);
                            if (!this.Map.Map.TryGetFirstValue(hash, out var item, out NativeParallelMultiHashMapIterator<int> iterator))
                            {
                                continue;
                            }

                            do
                            {
                                if (math.distancesq(query, this.Positions[item].Position) <= radiusSq)
                                {
                                    total++;
                                }
                            }
                            while (this.Map.Map.TryGetNextValue(out item, ref iterator));
                        }
                    }
                }

                this.Result.Value = total;
            }

            private static float CalculateSquareCellMinDistanceSq(float2 position, int2 cell)
            {
                var halfSize = new float2(WorldSize * 0.5f);
                var min = (new float2(cell.x, cell.y) * QuantizeStep) - halfSize;
                var max = min + QuantizeStep;
                var dx = math.max(0f, math.max(min.x - position.x, position.x - max.x));
                var dy = math.max(0f, math.max(min.y - position.y, position.y - max.y));
                return (dx * dx) + (dy * dy);
            }
        }

        [BurstCompile]
        private struct HexSpatialMapQueryJob : IJob
        {
            [ReadOnly]
            public NativeArray<float2> Queries;

            [ReadOnly]
            public NativeArray<TestSpatialPosition> Positions;

            [ReadOnly]
            public SpatialHexMap.ReadOnly Map;

            public float Radius;

            public NativeReference<int> Result;

            public void Execute()
            {
                var radiusSq = this.Radius * this.Radius;
                var total = 0;

                for (var i = 0; i < this.Queries.Length; i++)
                {
                    var query = this.Queries[i];
                    var center = this.Map.Quantized(query);
                    var searchRange = this.Map.SearchRange(this.Radius);

                    total += this.ProcessCell(query, center, radiusSq);

                    for (var ring = 1; ring <= searchRange; ring++)
                    {
                        var cell = center + (this.Map.Direction(4) * ring);

                        for (var side = 0; side < 6; side++)
                        {
                            var direction = this.Map.Direction(side);

                            for (var step = 0; step < ring; step++)
                            {
                                total += this.ProcessCell(query, cell, radiusSq);
                                cell += direction;
                            }
                        }
                    }
                }

                this.Result.Value = total;
            }

            private int ProcessCell(float2 query, int2 cell, float radiusSq)
            {
                if (!this.Map.IsWithinBounds(cell) || this.Map.CellMinDistanceSq(query, cell) > radiusSq)
                {
                    return 0;
                }

                var matches = 0;
                var key = this.Map.Hash(cell);
                if (!this.Map.Map.TryGetFirstValue(key, out var item, out NativeParallelMultiHashMapIterator<int> iterator))
                {
                    return 0;
                }

                do
                {
                    if (math.distancesq(query, this.Positions[item].Position) <= radiusSq)
                    {
                        matches++;
                    }
                }
                while (this.Map.Map.TryGetNextValue(out item, ref iterator));

                return matches;
            }
        }
    }
}
