// <copyright file="NativeArrayFactoryTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using System;
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class NativeArrayFactoryTests
    {
        [Test]
        public void CreateFromJob_ClearMemory_ReturnsOwnedArray()
        {
            var array = NativeArrayFactory<int>.CreateFromJob(4, Allocator.Persistent);

            try
            {
                Assert.IsTrue(array.IsCreated);
                Assert.AreEqual(4, array.Length);
                CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, array.ToArray());

                array[2] = 42;
                Assert.AreEqual(42, array[2]);
            }
            finally
            {
                array.Dispose();
            }
        }

        [Test]
        public void CreateFromJob_ZeroLength_ReturnsOwnedArray()
        {
            var array = NativeArrayFactory<int>.CreateFromJob(0, Allocator.Persistent);

            try
            {
                Assert.IsTrue(array.IsCreated);
                Assert.AreEqual(0, array.Length);
            }
            finally
            {
                array.Dispose();
            }
        }

        [Test]
        public void CreateFromJob_NegativeLength_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NativeArrayFactory<int>.CreateFromJob(-1, Allocator.Persistent));
        }

        [TestCase(Allocator.Invalid)]
        public void CreateFromJob_InvalidAllocator_Throws(Allocator allocator)
        {
            Assert.Throws<ArgumentException>(() => NativeArrayFactory<int>.CreateFromJob(1, allocator));
        }

        [Test]
        public void CreateFromJob_PersistentAllocationInsideBurstJob_Succeeds()
        {
            var result = new NativeArray<int>(2, Allocator.TempJob);

            try
            {
                new CreatePersistentArrayJob { Result = result }.Schedule().Complete();

                Assert.AreEqual(1, result[0]);
                Assert.AreEqual(42, result[1]);
            }
            finally
            {
                result.Dispose();
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct CreatePersistentArrayJob : IJob
        {
            public NativeArray<int> Result;

            public void Execute()
            {
                var array = NativeArrayFactory<int>.CreateFromJob(4, Allocator.Persistent);
                var isCleared = array[0] == 0 && array[1] == 0 && array[2] == 0 && array[3] == 0;

                array[2] = 42;
                this.Result[0] = isCleared ? 1 : 0;
                this.Result[1] = array[2];
                array.Dispose();
            }
        }
    }
}
