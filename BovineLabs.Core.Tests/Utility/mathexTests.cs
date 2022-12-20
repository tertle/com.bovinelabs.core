// <copyright file="mathexTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Mathematics;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "matching mathematics package")]
    [SuppressMessage("ReSharper", "SA1300", Justification = "matching mathematics package")]
    public class mathexTests
    {
        [Test]
        public void ShuffleMinTest()
        {
            var array = new NativeArray<int>(2, Allocator.Temp);
            array[0] = 2;
            array[1] = 4;
            var random = new Random(2);

            array.Shuffle(ref random);

            Assert.AreEqual(4, array[0]);
            Assert.AreEqual(2, array[1]);
        }

        [Test]
        public void AddTest()
        {
            const int length = 1000;

            var input = new NativeArray<int>(length, Allocator.Temp);
            var output = new NativeArray<int>(length, Allocator.Temp);
            var value = 5;

            var random = Random.CreateFromIndex(1234);
            for (var i = 0; i < input.Length; i++)
            {
                input[i] = random.NextInt(-10000, 10000);
            }

            mathex.add(output, input, value);

            for (var i = 0; i < input.Length; i++)
            {
                Assert.AreEqual(input[i] + value, output[i]);
            }
        }
    }
}
