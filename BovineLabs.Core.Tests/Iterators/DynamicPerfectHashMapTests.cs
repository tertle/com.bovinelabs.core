// <copyright file="DynamicPerfectHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public class DynamicPerfectHashMapTests : ECSTestsFixture
    {
        [Test]
        public void Initialize()
        {
            var input = new NativeHashMap<int, short>(5, Allocator.Temp);
            input.Add(1, 0);
            input.Add(98658, 1);
            input.Add(0, 2);
            input.Add(-6772, 3);
            input.Add(1234, 4);

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializePerfectHashMap<TestBuffer, int, short>(input, -1).AsPerfectHashMap<TestBuffer, int, short>();
            Assert.AreEqual((short)0, hashMap[1]);
            Assert.AreEqual((short)1, hashMap[98658]);
            Assert.AreEqual((short)2, hashMap[0]);
            Assert.AreEqual((short)3, hashMap[-6772]);
            Assert.AreEqual((short)4, hashMap[1234]);
        }

        [Test]
        public void TryGetValue_MissingKey_ReturnsFalse()
        {
            var input = new NativeHashMap<int, short>(1, Allocator.Temp);
            input.Add(123, 7);

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializePerfectHashMap<TestBuffer, int, short>(input, -1).AsPerfectHashMap<TestBuffer, int, short>();

            Assert.IsFalse(hashMap.TryGetValue(999, out _));
            Assert.IsFalse(hashMap.ContainsKey(999));
        }

        [Test]
        public void ThrowsOnCollision()
        {
            var keys = new NativeArray<int>(5, Allocator.Temp);
            keys[0] = 0;
            keys[1] = 1;
            keys[2] = 2;
            keys[3] = 3;
            keys[4] = 0;

            var values = new NativeArray<short>(5, Allocator.Temp);
            values[0] = 0;
            values[1] = 1;
            values[2] = 2;
            values[3] = 3;
            values[4] = 4;

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            Assert.Throws<ArgumentException>(() => buffer.InitializePerfectHashMap<TestBuffer, int, short>(keys, values, -1));
        }

        [Test]
        public unsafe void ValuesPointer_IsAlignedToValueType()
        {
            var keys = new NativeArray<byte>(2, Allocator.Temp);
            keys[0] = 0;
            keys[1] = 1;

            var values = new NativeArray<long>(2, Allocator.Temp);
            values[0] = 10;
            values[1] = 20;

            var entity = this.Manager.CreateEntity(typeof(TestBufferByteLong));
            var buffer = this.Manager.GetBuffer<TestBufferByteLong>(entity);

            var hashMap = buffer.InitializePerfectHashMap(keys, values, long.MinValue)
                .AsPerfectHashMap<TestBufferByteLong, byte, long>();

            var align = UnsafeUtility.AlignOf<long>();
            var valuesPtr = (ulong)hashMap.Helper->Values;
            Assert.AreEqual(0u, valuesPtr % (ulong)align);
        }

        [Test]
        public void Initialize_WithMismatchedKeysAndValues_ThrowsArgumentException()
        {
            var keys = new NativeArray<int>(2, Allocator.Temp);
            keys[0] = 1;
            keys[1] = 2;

            var values = new NativeArray<short>(1, Allocator.Temp);
            values[0] = 3;

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            Assert.Throws<ArgumentException>(() => buffer.InitializePerfectHashMap(keys, values, (short)-1));
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IDynamicPerfectHashMap<int, short>
        {
            byte IDynamicPerfectHashMap<int, short>.Value { get; }
        }

        [InternalBufferCapacity(0)]
        private struct TestBufferByteLong : IDynamicPerfectHashMap<byte, long>
        {
            byte IDynamicPerfectHashMap<byte, long>.Value { get; }
        }
    }
}
