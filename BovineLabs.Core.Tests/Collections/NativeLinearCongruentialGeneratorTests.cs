// <copyright file="NativeLinearCongruentialGeneratorTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Jobs;
    using UnityEngine;

    public class NativeLinearCongruentialGeneratorTests
    {
        [Test]
        public void Unique()
        {
            default(Test).Run();
        }

        [BurstCompile(DisableSafetyChecks = true, CompileSynchronously = true)]
        private struct Test : IJob
        {
            public void Execute()
            {
                const int length = 1 << 20;

                var hashset = new NativeParallelHashSet<int>(length, Allocator.Temp);

                var lcg = new NativeLinearCongruentialGenerator(123456, Allocator.Temp);
                for (var i = 0; i < length; i++)
                {
                    var v = lcg.Next();
                    hashset.Add(v);
                }

                Debug.Assert(hashset.Count() == length);
            }
        }
    }
}
