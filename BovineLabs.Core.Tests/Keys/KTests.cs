// <copyright file="KTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Keys
{
    using BovineLabs.Core.Keys;
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
                new NameValue
                {
                    Name = "test1",
                    Value = 1,
                },
                new NameValue
                {
                    Name = "test2",
                    Value = 2,
                },
                new NameValue
                {
                    Name = "test3",
                    Value = 3,
                },
                new NameValue
                {
                    Name = "test4",
                    Value = 4,
                },
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
            using var result = new NativeReference<uint>(Allocator.TempJob);

            new BurstTest { Result = result }.Schedule().Complete();

            Assert.AreEqual(result.Value, 4);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BurstTest : IJob
        {
            public NativeReference<uint> Result;

            public void Execute()
            {
                this.Result.Value = (byte)K<KTests>.NameToKey("test4");
            }
        }
    }
}
