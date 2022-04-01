// <copyright file="KTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Settings
{
    using BovineLabs.Core.Variables;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class KTests
    {

        [SetUp]
        public void Setup()
        {
            var kvp = new[]
            {
                new NameValue { Name = "test1", Value = new ByteReference(1) },
                new NameValue { Name = "test2", Value = new ByteReference(2) },
                new NameValue { Name = "test3", Value = new ByteReference(3) },
                new NameValue { Name = "test4", Value = new ByteReference(4) },
            };

            K<KTests>.Initialize(kvp);
        }

        [Test]
        public void StringTest()
        {
            Assert.AreEqual(K<KTests>.NameToKey("test2"), 2);
        }

        [Test]
        public void StringWorksInBurst()
        {
            var result = new NativeReference<uint>(Allocator.TempJob);

            new BurstTest { Result = result }.Schedule().Complete();

            Assert.AreEqual(result.Value, 4);

            result.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstTest : IJob
        {
            public NativeReference<uint> Result;

            public void Execute()
            {
                this.Result.Value = K<KTests>.NameToKey("test4");
            }
        }
    }
}
