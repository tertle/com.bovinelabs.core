// <copyright file="ProfilerRecorderGroupTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using System;
    using System.Collections;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Profiling;
    using UnityEngine.TestTools;

    public class ProfilerRecorderGroupTests
    {
        [Test]
        public void Constructor_WithoutCounterNames_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ProfilerRecorderGroup(ProfilerCategory.Scripts));
        }

        [UnityTest]
        public IEnumerator LastValue_SumsAvailableCounters()
        {
            var group = new ProfilerRecorderGroup(ProfilerCategory.Render, "Triangles Count", "Vertices Count");
            var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            var vertices = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 1);

            try
            {
                yield return null;
                Assert.AreEqual(triangles.LastValue + vertices.LastValue, group.LastValue);
            }
            finally
            {
                group.Dispose();
                triangles.Dispose();
                vertices.Dispose();
            }
        }

        [Test]
        public void Dispose_CanBeCalledMoreThanOnce()
        {
            var group = new ProfilerRecorderGroup(ProfilerCategory.Render, "Triangles Count");

            group.Dispose();

            Assert.DoesNotThrow(group.Dispose);
            Assert.IsFalse(group.Valid);
        }
    }
}
