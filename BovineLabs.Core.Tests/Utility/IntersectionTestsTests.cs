// <copyright file="IntersectionTestsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Mathematics;

    public class IntersectionTestsTests
    {
        [Test]
        public void AABBTriangle()
        {
            var aabb = new MinMaxAABB
            {
                Min = new float3(1),
                Max = new float3(5),
            };

            Assert.IsTrue(IntersectionTests.AABBTriangle(aabb, new float3(3, 0, 3), new float3(3, 4, 3), new float3(4, 0, 3)));
            Assert.IsTrue(IntersectionTests.AABBTriangle(aabb, new float3(2, 2, 3), new float3(2, 3, 3), new float3(3, 2, 3)));
            Assert.IsFalse(IntersectionTests.AABBTriangle(aabb, new float3(0, 0, 3), new float3(-1, -1, 3), new float3(-1, 0, 3)));
        }
    }
}
