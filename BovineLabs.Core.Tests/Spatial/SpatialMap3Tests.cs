// <copyright file="SpatialMap3Tests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Spatial
{
    using BovineLabs.Core.Spatial;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    public class SpatialMap3Tests
    {
        private const float QuantizeStep = 10f;
        private const int WorldSize = 100;
        private const float Tolerance = 0.0001f;

        [Test]
        public void BoundsHelpers_ClampAndRejectBothSides()
        {
            const int width = WorldSize / (int)QuantizeStep;

            Assert.IsTrue(SpatialMap3.IsWithinBounds(int3.zero, width));
            Assert.IsTrue(SpatialMap3.IsWithinBounds(new int3(width - 1), width));
            Assert.IsFalse(SpatialMap3.IsWithinBounds(new int3(-1, 0, 0), width));
            Assert.IsFalse(SpatialMap3.IsWithinBounds(new int3(width, width - 1, width - 1), width));

            Assert.AreEqual(int3.zero, SpatialMap3.Clamp(new int3(-4, -1, -2), width));
            Assert.AreEqual(new int3(width - 1), SpatialMap3.Clamp(new int3(width + 4), width));
        }

        [Test]
        public void CellMinDistanceSqXZ_IgnoresHeightAndReturnsExactAabbDistance()
        {
            var halfSize = new int3(WorldSize / 2);
            var cell = new int3(5, 2, 5); // XZ world-space AABB [0, 10] x [0, 10].

            Assert.AreEqual(0f, SpatialMap3.CellMinDistanceSqXZ(new float2(3f, 8f), cell, QuantizeStep, halfSize), Tolerance);
            Assert.AreEqual(4f, SpatialMap3.CellMinDistanceSqXZ(new float2(-2f, 4f), cell, QuantizeStep, halfSize), Tolerance);
            Assert.AreEqual(25f, SpatialMap3.CellMinDistanceSqXZ(new float2(-3f, 14f), cell, QuantizeStep, halfSize), Tolerance);
        }

        [Test]
        public void Build_CanCopyCompactXZPositionsDuringQuantization()
        {
            var map = new SpatialMap3<TestSpatialPosition3>(QuantizeStep, WorldSize);
            var positions = new NativeArray<TestSpatialPosition3>(3, Allocator.TempJob);
            var positionOutput = new NativeArray<float2>(positions.Length, Allocator.TempJob);

            try
            {
                positions[0] = new TestSpatialPosition3 { Position = new float3(-12f, 4f, 9f) };
                positions[1] = new TestSpatialPosition3 { Position = new float3(0.5f, 8f, 17f) };
                positions[2] = new TestSpatialPosition3 { Position = new float3(17f, -21f, -6f) };

                map.BuildWithPositionOutput(positions, positionOutput, default).Complete();

                for (var i = 0; i < positions.Length; i++)
                {
                    Assert.AreEqual(positions[i].Position.xz, positionOutput[i]);
                }
            }
            finally
            {
                positionOutput.Dispose();
                positions.Dispose();
                map.Dispose();
            }
        }

        [Test]
        public void Clamp_PreventsOutOfBoundsCellsAliasingInBoundsHashes()
        {
            const int width = WorldSize / (int)QuantizeStep;
            var leftOfBounds = new int3(-1, 1, 0);
            var rightOfBounds = new int3(width, 0, 0);

            Assert.AreEqual(SpatialMap3.Hash(new int3(width - 1, 0, 0), width, width), SpatialMap3.Hash(leftOfBounds, width, width));
            Assert.AreEqual(SpatialMap3.Hash(new int3(0, 1, 0), width, width), SpatialMap3.Hash(rightOfBounds, width, width));

            Assert.AreNotEqual(
                SpatialMap3.Hash(new int3(width - 1, 0, 0), width, width),
                SpatialMap3.Hash(SpatialMap3.Clamp(leftOfBounds, width), width, width));
            Assert.AreNotEqual(
                SpatialMap3.Hash(new int3(0, 1, 0), width, width),
                SpatialMap3.Hash(SpatialMap3.Clamp(rightOfBounds, width), width, width));
        }

        [Test]
        public void ReadOnlyHelpers_UseConfiguredMapGeometry()
        {
            var map = new SpatialMap3<TestSpatialPosition3>(QuantizeStep, WorldSize);

            try
            {
                var readOnly = map.AsReadOnly();
                var centerCell = readOnly.Quantized(float3.zero);

                Assert.AreEqual(new int3(5, 5, 5), centerCell);
                Assert.IsTrue(readOnly.IsWithinBounds(centerCell));
                Assert.IsFalse(readOnly.IsWithinBounds(new int3(-1, centerCell.y, centerCell.z)));
                Assert.AreEqual(int3.zero, readOnly.Clamp(new int3(-2, -3, -4)));
                Assert.AreEqual(0f, readOnly.CellMinDistanceSqXZ(new float2(4f, 7f), centerCell), Tolerance);
            }
            finally
            {
                map.Dispose();
            }
        }

        private struct TestSpatialPosition3 : ISpatialPosition3
        {
            public float3 Position { get; set; }
        }
    }
}
