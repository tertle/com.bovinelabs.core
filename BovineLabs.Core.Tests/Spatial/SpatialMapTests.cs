// <copyright file="SpatialMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Spatial
{
    using BovineLabs.Core.Spatial;
    using NUnit.Framework;
    using Unity.Mathematics;

    public class SpatialMapTests
    {
        private const float QuantizeStep = 10f;
        private const int WorldSize = 100;
        private const float Tolerance = 0.0001f;

        [Test]
        public void BoundsHelpers_ClampAndRejectBothSides()
        {
            const int width = WorldSize / (int)QuantizeStep;

            Assert.IsTrue(SpatialMap.IsWithinBounds(int2.zero, width));
            Assert.IsTrue(SpatialMap.IsWithinBounds(new int2(width - 1), width));
            Assert.IsFalse(SpatialMap.IsWithinBounds(new int2(-1, 0), width));
            Assert.IsFalse(SpatialMap.IsWithinBounds(new int2(width, width - 1), width));

            Assert.AreEqual(int2.zero, SpatialMap.Clamp(new int2(-4, -1), width));
            Assert.AreEqual(new int2(width - 1), SpatialMap.Clamp(new int2(width + 4), width));
        }

        [Test]
        public void CellMinDistanceSq_ReturnsExactAabbDistance()
        {
            var halfSize = new int2(WorldSize / 2);
            var cell = new int2(5, 5); // World-space AABB [0, 10] x [0, 10].

            Assert.AreEqual(0f, SpatialMap.CellMinDistanceSq(new float2(3f, 8f), cell, QuantizeStep, halfSize), Tolerance);
            Assert.AreEqual(4f, SpatialMap.CellMinDistanceSq(new float2(-2f, 4f), cell, QuantizeStep, halfSize), Tolerance);
            Assert.AreEqual(25f, SpatialMap.CellMinDistanceSq(new float2(-3f, 14f), cell, QuantizeStep, halfSize), Tolerance);
        }

        [Test]
        public void Clamp_PreventsOutOfBoundsCellsAliasingInBoundsHashes()
        {
            const int width = WorldSize / (int)QuantizeStep;
            var leftOfBounds = new int2(-1, 1);
            var rightOfBounds = new int2(width, 0);

            Assert.AreEqual(SpatialMap.Hash(new int2(width - 1, 0), width), SpatialMap.Hash(leftOfBounds, width));
            Assert.AreEqual(SpatialMap.Hash(new int2(0, 1), width), SpatialMap.Hash(rightOfBounds, width));

            Assert.AreNotEqual(SpatialMap.Hash(new int2(width - 1, 0), width), SpatialMap.Hash(SpatialMap.Clamp(leftOfBounds, width), width));
            Assert.AreNotEqual(SpatialMap.Hash(new int2(0, 1), width), SpatialMap.Hash(SpatialMap.Clamp(rightOfBounds, width), width));
        }

        [Test]
        public void ReadOnlyHelpers_UseConfiguredMapGeometry()
        {
            var map = new SpatialMap<TestSpatialPosition>(QuantizeStep, WorldSize);

            try
            {
                var readOnly = map.AsReadOnly();
                var centerCell = readOnly.Quantized(float2.zero);

                Assert.AreEqual(new int2(5, 5), centerCell);
                Assert.IsTrue(readOnly.IsWithinBounds(centerCell));
                Assert.IsFalse(readOnly.IsWithinBounds(new int2(-1, centerCell.y)));
                Assert.AreEqual(int2.zero, readOnly.Clamp(new int2(-2, -3)));
                Assert.AreEqual(0f, readOnly.CellMinDistanceSq(new float2(4f, 7f), centerCell), Tolerance);
            }
            finally
            {
                map.Dispose();
            }
        }

        private struct TestSpatialPosition : ISpatialPosition
        {
            public float2 Position { get; set; }
        }
    }
}
