// <copyright file="ConfigVarSharedStaticRectContainerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using NUnit.Framework;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticRectContainerTests
    {
        [Test]
        public void StringValueRoundTripsAllRectComponents()
        {
            var field = SharedStatic<Rect>.GetOrCreate<TestContext>();
            IConfigVarContainer<Rect> container = new ConfigVarSharedStaticRectContainer(field);
            var expected = new Rect(1.25f, -2.5f, 3.75f, 4.5f);

            container.Value = expected;
            var serialized = container.StringValue;
            container.Value = default;
            container.StringValue = serialized;

            Assert.AreEqual(expected, container.Value);
        }

        private struct TestContext
        {
        }
    }
}
