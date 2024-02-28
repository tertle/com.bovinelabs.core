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
    using Assert = Unity.Assertions.Assert;

    [BurstCompile]
    public class FunctionTests
    {
        private static readonly Entity Expected = new() { Index = 1234, Version = 5 };

        [Test]
        public void ExecuteTest()
        {
            SystemState state = default;

            var functions = new FunctionsBuilder<TestData, Entity>(Allocator.Temp)
                .Add<TestFunction>(ref state)
                .Build();

            var data = new TestData { Value = Expected };
            var actual = functions.Execute(0, ref data);
            Assert.AreEqual(Expected, actual);

            functions.OnDestroy(ref state);
        }

        [Test]
        public void ReflectAll()
        {
            SystemState state = default;

            var functions = new FunctionsBuilder<TestData, Entity>(Allocator.Temp)
                .ReflectAll(ref state)
                .Build();

            var data = new TestData { Value = Expected };
            var actual = functions.Execute(0, ref data);
            Assert.AreEqual(Expected, actual);

            functions.OnDestroy(ref state);
        }

        [Test]
        public void ReflectAllCache()
        {
            SystemState state = default;

            var functions = new FunctionsBuilder<TestData, Entity>(Allocator.Temp).ReflectAll(ref state).Build();
            var functions2 = new FunctionsBuilder<TestData, Entity>(Allocator.Temp).ReflectAll(ref state).Build();

            var data = new TestData { Value = Expected };
            var actual = functions.Execute(0, ref data);
            var actual2 = functions2.Execute(0, ref data);

            Assert.AreEqual(Expected, actual);
            Assert.AreEqual(Expected, actual2);

            functions.OnDestroy(ref state);
            functions2.OnDestroy(ref state);
        }

        private struct TestData
        {
            public Entity Value;
        }

        [BurstCompile]
        private unsafe struct TestFunction : IFunction<TestData>
        {
            public UpdateFunction? UpdateFunction => null;

            public DestroyFunction? DestroyFunction => null;

            public ExecuteFunction ExecuteFunction => Execute;

            public void OnCreate(ref SystemState state)
            {
            }

            private Entity Execute(ref TestData data)
            {
                return data.Value;
            }

            [BurstCompile]
            [AOT.MonoPInvokeCallback(typeof(ExecuteFunction))]
            private static void Execute(void* target, void* data, void* result)
            {
                *(Entity*)result = ((TestFunction*)target)->Execute(ref UnsafeUtility.AsRef<TestData>(data));
            }
        }
    }
}
