// <copyright file="FunctionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Functions
{
    using BovineLabs.Core.Functions;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using UnityEngine;
    using Assert = Unity.Assertions.Assert;

    [BurstCompile]
    public class FunctionTests
    {
        private const int Expected = 7;

        [Test]
        public void ExecuteTest()
        {
            SystemState state = default;

            var functions = new FunctionsBuilder<TestData>(Allocator.Temp)
                .Add<TestFunction>(ref state)
                .Build();

            var data = new TestData { Value = 5 };
            var actual = functions.Execute(0, ref data);
            Assert.AreEqual(Expected, actual);

            functions.OnDestroy(ref state);
        }

        [Test]
        public void ReflectAll()
        {
            const int expected = 7;

            SystemState state = default;

            var functions = new FunctionsBuilder<TestData>(Allocator.Temp)
                .ReflectAll(ref state)
                .Build();

            var data = new TestData { Value = 7 };
            var actual = functions.Execute(0, ref data);
            Assert.AreEqual(expected, actual);

            functions.OnDestroy(ref state);
        }

        [Test]
        public void ReflectAllCache()
        {
            const int expected = 7;

            SystemState state = default;

            var functions = new FunctionsBuilder<TestData>(Allocator.Temp).ReflectAll(ref state).Build();
            var functions2 = new FunctionsBuilder<TestData>(Allocator.Temp).ReflectAll(ref state).Build();

            var data = new TestData { Value = 5 };
            var actual = functions.Execute(0, ref data);
            var actual2 = functions2.Execute(0, ref data);

            Assert.AreEqual(expected, actual);
            Assert.AreEqual(expected, actual2);

            functions.OnDestroy(ref state);
            functions2.OnDestroy(ref state);
        }

        private struct TestData
        {
            public int Value;
        }

        [BurstCompile]
        private unsafe struct TestFunction : IFunction<TestData>
        {
            private int result;

            public UpdateFunction? UpdateFunction => null;

            public DestroyFunction? DestroyFunction => null;

            public ExecuteFunction ExecuteFunction => Execute;

            public void OnCreate(ref SystemState state)
            {
                this.result = Expected;
            }

            private int Execute(ref TestData data)
            {
                return this.result;
            }

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(ExecuteFunction))]
            private static int Execute(void* target, void* data)
            {
                return ((TestFunction*)target)->Execute(ref UnsafeUtility.AsRef<TestData>(data));
            }
        }
    }
}
