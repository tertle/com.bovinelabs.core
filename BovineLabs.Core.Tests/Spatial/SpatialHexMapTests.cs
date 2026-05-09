// <copyright file="HexSpatialMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Spatial
{
    using System.Collections.Generic;
    using BovineLabs.Core.Spatial;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    public class SpatialHexMapTests
    {
        private const float QuantizeStep = 16f;
        private const int WorldSize = 64;
        private const float Tolerance = 0.001f;

        [Test]
        public void Quantized_CanonicalPositions_ReturnExpectedAxialCells()
        {
            var outerRadius = QuantizeStep / math.sqrt(3f);

            Assert.AreEqual(new int2(0, 0), SpatialHexMap.Quantized(float2.zero, outerRadius));
            Assert.AreEqual(new int2(1, 0), SpatialHexMap.Quantized(SpatialHexMap.Center(new int2(1, 0), outerRadius), outerRadius));
            Assert.AreEqual(new int2(0, 1), SpatialHexMap.Quantized(SpatialHexMap.Center(new int2(0, 1), outerRadius), outerRadius));
            Assert.AreEqual(new int2(-1, 1), SpatialHexMap.Quantized(SpatialHexMap.Center(new int2(-1, 1), outerRadius), outerRadius));
        }

        [Test]
        public void Center_OfQuantizedPosition_StaysInExpectedNeighbourhood()
        {
            var outerRadius = QuantizeStep / math.sqrt(3f);
            var positions = new[]
            {
                new float2(0f, 0f),
                new float2(6f, 4f),
                new float2(-7f, 3f),
                SpatialHexMap.Center(new int2(2, -1), outerRadius) + new float2(0.25f, -0.5f),
            };

            foreach (var position in positions)
            {
                var axial = SpatialHexMap.Quantized(position, outerRadius);
                var center = SpatialHexMap.Center(axial, outerRadius);

                Assert.LessOrEqual(math.distance(position, center), outerRadius + Tolerance);
            }
        }

        [Test]
        public void Hash_ProducesUniquePackedKeys_WithinBounds()
        {
            var boundsMin = new int2(-2, -1);
            const int boundsWidth = 5;

            var hashes = new HashSet<int>();
            for (var y = -1; y <= 2; y++)
            {
                for (var x = -2; x <= 2; x++)
                {
                    var added = hashes.Add(SpatialHexMap.Hash(new int2(x, y), boundsMin, boundsWidth));
                    Assert.IsTrue(added, $"Hash collision at ({x}, {y})");
                }
            }
        }

        [Test]
        public void IsWithinBounds_ReturnsExpectedValues_AtEdges()
        {
            var boundsMin = new int2(-2, -1);
            var boundsSize = new int2(5, 4);

            Assert.IsTrue(SpatialHexMap.IsWithinBounds(boundsMin, boundsMin, boundsSize));
            Assert.IsTrue(SpatialHexMap.IsWithinBounds(boundsMin + boundsSize - 1, boundsMin, boundsSize));
            Assert.IsFalse(SpatialHexMap.IsWithinBounds(boundsMin - 1, boundsMin, boundsSize));
            Assert.IsFalse(SpatialHexMap.IsWithinBounds(boundsMin + boundsSize, boundsMin, boundsSize));
        }

        [Test]
        public void ConstructorBounds_AcceptAllWorldCorners()
        {
            var map = new SpatialHexMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                var readOnly = map.AsReadOnly();
                var half = WorldSize / 2f;
                var corners = new[]
                {
                    new float2(-half, -half),
                    new float2(-half, half),
                    new float2(half, -half),
                    new float2(half, half),
                };

                foreach (var corner in corners)
                {
                    var axial = readOnly.Quantized(corner);
                    Assert.IsTrue(readOnly.IsWithinBounds(axial), $"Corner {corner} quantized to {axial}");
                }
            }
            finally
            {
                map.Dispose();
            }
        }

        [Test]
        public void Build_InsertsExpectedIndicesIntoExpectedCells()
        {
            var map = new SpatialHexMap<TestSpatialPosition>(QuantizeStep, WorldSize);
            var outerRadius = QuantizeStep / math.sqrt(3f);
            var positions = new NativeArray<TestSpatialPosition>(4, Allocator.TempJob);

            try
            {
                positions[0] = new TestSpatialPosition { Position = SpatialHexMap.Center(new int2(0, 0), outerRadius) };
                positions[1] = new TestSpatialPosition { Position = SpatialHexMap.Center(new int2(1, 0), outerRadius) };
                positions[2] = new TestSpatialPosition { Position = SpatialHexMap.Center(new int2(0, 0), outerRadius) + new float2(0.25f, -0.5f) };
                positions[3] = new TestSpatialPosition { Position = SpatialHexMap.Center(new int2(-1, 1), outerRadius) };

                map.Build(positions, default).Complete();

                var readOnly = map.AsReadOnly();

                CollectionAssert.AreEquivalent(new[] { 0, 2 }, CollectIndices(readOnly, new int2(0, 0)));
                CollectionAssert.AreEquivalent(new[] { 1 }, CollectIndices(readOnly, new int2(1, 0)));
                CollectionAssert.AreEquivalent(new[] { 3 }, CollectIndices(readOnly, new int2(-1, 1)));
            }
            finally
            {
                positions.Dispose();
                map.Dispose();
            }
        }

        [Test]
        public void Direction_ReturnsCanonicalOffsets()
        {
            Assert.AreEqual(new int2(1, 0), SpatialHexMap.Direction(0));
            Assert.AreEqual(new int2(1, -1), SpatialHexMap.Direction(1));
            Assert.AreEqual(new int2(0, -1), SpatialHexMap.Direction(2));
            Assert.AreEqual(new int2(-1, 0), SpatialHexMap.Direction(3));
            Assert.AreEqual(new int2(-1, 1), SpatialHexMap.Direction(4));
            Assert.AreEqual(new int2(0, 1), SpatialHexMap.Direction(5));
        }

        [Test]
        public void SearchRange_CoversAllCellsFromConservativeCenterOracle()
        {
            var outerRadius = QuantizeStep / math.sqrt(3f);
            var queryPosition = new float2(3f, -2f);
            const float radius = 18f;
            var center = SpatialHexMap.Quantized(queryPosition, outerRadius);
            var searchRange = SpatialHexMap.SearchRange(radius, outerRadius);

            var visited = EnumerateRingCells(center, searchRange);
            var limit = searchRange + 3;

            for (var y = -limit; y <= limit; y++)
            {
                for (var x = -limit; x <= limit; x++)
                {
                    var cell = center + new int2(x, y);
                    var centerDistance = math.distance(queryPosition, SpatialHexMap.Center(cell, outerRadius));
                    if (centerDistance > radius + outerRadius + Tolerance)
                    {
                        continue;
                    }

                    Assert.Contains(Pack(cell), visited, $"Ring traversal missed cell {cell}");
                }
            }
        }

        [Test]
        public void CellMinDistanceSq_ReturnsZeroInsideAndPositiveOutside()
        {
            var outerRadius = QuantizeStep / math.sqrt(3f);
            var cell = new int2(2, -1);
            var center = SpatialHexMap.Center(cell, outerRadius);

            Assert.AreEqual(0f, SpatialHexMap.CellMinDistanceSq(center, cell, outerRadius), Tolerance);
            Assert.Greater(SpatialHexMap.CellMinDistanceSq(center + new float2(outerRadius * 3f, 0f), cell, outerRadius), 0f);
        }

        private static List<int> CollectIndices(SpatialHexMap.ReadOnly map, int2 cell)
        {
            var results = new List<int>();
            var hash = map.Hash(cell);

            if (!map.Map.TryGetFirstValue(hash, out var item, out NativeParallelMultiHashMapIterator<int> iterator))
            {
                return results;
            }

            do
            {
                results.Add(item);
            }
            while (map.Map.TryGetNextValue(out item, ref iterator));

            return results;
        }

        private static List<long> EnumerateRingCells(int2 center, int ringCount)
        {
            var cells = new List<long> { Pack(center) };

            for (var ring = 1; ring <= ringCount; ring++)
            {
                var cell = center + (SpatialHexMap.Direction(4) * ring);

                for (var side = 0; side < 6; side++)
                {
                    var direction = SpatialHexMap.Direction(side);

                    for (var step = 0; step < ring; step++)
                    {
                        cells.Add(Pack(cell));
                        cell += direction;
                    }
                }
            }

            return cells;
        }

        private static long Pack(int2 cell)
        {
            return ((long)cell.x << 32) | (uint)cell.y;
        }

        private struct TestSpatialPosition : ISpatialPosition
        {
            public float2 Position { get; set; }
        }
    }
}
