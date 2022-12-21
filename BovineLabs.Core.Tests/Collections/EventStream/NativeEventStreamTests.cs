// <copyright file="NativeEventStreamTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_TESTING
namespace BovineLabs.Core.Tests.Collections.EventStream
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    /// <summary> Tests for <see cref="NativeEventStream" /> . </summary>
    internal partial class NativeEventStreamTests : ECSTestsFixture
    {
        /// <summary> Tests that you can create and destroy. </summary>
        [Test]
        public void CreateAndDestroy()
        {
            var stream = new NativeEventStream(Allocator.TempJob);

            Assert.IsTrue(stream.IsCreated);
            Assert.IsTrue(stream.Count() == 0);

            stream.Dispose();
            Assert.IsFalse(stream.IsCreated);
        }
    }
}

#endif
