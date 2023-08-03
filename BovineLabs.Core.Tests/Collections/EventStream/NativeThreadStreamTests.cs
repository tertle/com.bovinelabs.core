// <copyright file="NativeThreadStreamTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_TESTING
namespace BovineLabs.Core.Tests.Collections.EventStream
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;

    /// <summary> Tests for <see cref="NativeThreadStream" /> . </summary>
    internal partial class NativeThreadStreamTests : ECSTestsFixture
    {
        /// <summary> Tests that you can create and destroy. </summary>
        [Test]
        public void CreateAndDestroy()
        {
            var stream = new NativeThreadStream(Allocator.TempJob);

            Assert.IsTrue(stream.IsCreated);
            Assert.IsTrue(stream.Count() == 0);

            stream.Dispose();
            Assert.IsFalse(stream.IsCreated);
        }
    }
}

#endif
