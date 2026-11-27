namespace BovineLabs.Core.Tests.Collections
{
    using System.Numerics;
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;

    public class BitArrayTests
    {
        [Test]
        public void GenericOperatorsCombineBothWordsInBurstJob()
        {
            using var results = new NativeArray<BitArray128>(4, Allocator.TempJob);
            new BitwiseJob<BitArray128>
            {
                Left = new BitArray128(0x0f, 0xf0),
                Right = new BitArray128(0x33, 0xca),
                Results = results,
            }.Run();

            Assert.AreEqual(new BitArray128(0x03, 0xc0), results[0]);
            Assert.AreEqual(new BitArray128(0x3f, 0xfa), results[1]);
            Assert.AreEqual(new BitArray128(0x3c, 0x3a), results[2]);
            Assert.AreEqual(new BitArray128(~0x0fUL, ~0xf0UL), results[3]);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct BitwiseJob<T> : IJob
            where T : unmanaged, IBitwiseOperators<T, T, T>
        {
            public T Left;
            public T Right;
            public NativeArray<T> Results;

            public void Execute()
            {
                Results[0] = Left & Right;
                Results[1] = Left | Right;
                Results[2] = Left ^ Right;
                Results[3] = ~Left;
            }
        }
    }
}
