// <copyright file="MathematicsExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Mathematics;
    using Assert = UnityEngine.Assertions.Assert;

    /// <summary> Tests for <see cref="MathematicsExtensions" />. </summary>
    public class MathematicsExtensionsTests
    {
        /// <summary> Tests for the AABB extension methods. </summary>
        public class AABBTests
        {
            /// <summary> Tests <see cref="MathematicsExtensions.Expand" />. </summary>
            [Test]
            public void Expand()
            {
                var aabb = default(AABB);
                aabb.Center = new float3(10, 15, 20);
                aabb.Extents = new float3(1, 2, 3);

                var aabb2 = aabb.Expand(2);

                Assert.AreEqual(aabb.Center, aabb2.Center);
                Assert.AreEqual(aabb.Size + new float3(2), aabb2.Size);
            }

            /// <summary> Tests <see cref="MathematicsExtensions.IsDefault" />. </summary>
            [Test]
            public void IsDefault()
            {
                var aabb = default(AABB);

                Assert.IsTrue(aabb.IsDefault());

                aabb = default;
                aabb.Center = new float3(10, 15, 20);

                Assert.IsFalse(aabb.IsDefault());

                aabb = default;
                aabb.Extents = new float3(1, 2, 3);

                Assert.IsFalse(aabb.IsDefault());

                aabb = default;
                aabb.Center = new float3(10, 15, 20);
                aabb.Extents = new float3(1, 2, 3);

                Assert.IsFalse(aabb.IsDefault());
            }

            /// <summary> Tests <see cref="MathematicsExtensions.Encapsulate" />. </summary>
            [Test]
            public void Encapsulate()
            {
                var aabb = default(AABB);
                aabb.Center = new float3(10, 10, 10);
                aabb.Extents = new float3(2, 2, 2);

                var aabb2 = default(AABB);
                aabb2.Extents = new float3(1, 1, 1);

                var aabb3 = aabb.Encapsulate(aabb2);

                Assert.AreEqual(new float3(-1, -1, -1), aabb3.Min);
                Assert.AreEqual(new float3(12, 12, 12), aabb3.Max);
            }
        }
    }
}
