// <copyright file="MeshSimplifierTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Collections;
    using UnityEngine;

    public class MeshSimplifierTests
    {
        [Test]
        public void Simplify_QualityOne_PreservesTriangleCount()
        {
            using var vertices = new NativeArray<Vector3>(
                new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 0, 1),
                    new Vector3(0, 0, 1),
                },
                Allocator.Temp);
            using var indices = new NativeArray<int>(new[] { 0, 1, 2, 0, 2, 3 }, Allocator.Temp);
            var options = new MeshSimplifier.Options(1);
            var allocator = Allocator.Temp;

            MeshSimplifier.Simplify(in vertices, in indices, in options, in allocator, out var result);
            try
            {
                using var simplifiedVertices = result.GetVertices(Allocator.Temp);
                using var simplifiedIndices = result.GetIndices(Allocator.Temp);

                Assert.AreEqual(vertices.Length, simplifiedVertices.Length);
                Assert.AreEqual(indices.Length, simplifiedIndices.Length);
                for (var i = 0; i < simplifiedIndices.Length; i++)
                {
                    Assert.That(simplifiedIndices[i], Is.InRange(0, simplifiedVertices.Length - 1));
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void TriangleError_LargeValue_DoesNotOverflow()
        {
            const double expected = 100_000;
            var triangle = new MeshSimplifier.Triangle(0, 0, 1, 2) { Err0 = expected };

            Assert.That(triangle.Err0, Is.EqualTo(expected).Within(1));
        }
    }
}
