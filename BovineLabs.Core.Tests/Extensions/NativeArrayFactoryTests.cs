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
                Result[0] = isCleared ? 1 : 0;
                Result[1] = array[2];
                array.Dispose();
            }
        }
    }
}
