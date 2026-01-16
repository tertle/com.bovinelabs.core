// <copyright file="DynamicUntypedHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public class DynamicUntypedHashMapTests : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();

            TypeManagerEx.Initialize();
        }

        [Test]
        public void InitializeAndLarge()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            hashMap.AddOrSet(0, 1);
            hashMap.AddOrSet(1, (short)2);
            hashMap.AddOrSet(2, new float3(1, 2, 3));
            hashMap.AddOrSet(3, new Large());
            hashMap.AddOrSet(2, new float3(3, 2, 1));
            hashMap.AddOrSet(3, new Large());

            Assert.AreEqual(1, hashMap.GetOrAddRef(0, 0));
            Assert.AreEqual((short)2, hashMap.GetOrAddRef(1, (short)0));
            Assert.AreEqual(new float3(3, 2, 1), hashMap.GetOrAddRef(2, float3.zero));
        }

        [Test]
        public void StressAdd()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            for (var i = 0; i < 50; i++)
            {
                hashMap.AddOrSet((i * 8) + 0, 1);
                hashMap.AddOrSet((i * 8) + 1, (short)2);
                hashMap.AddOrSet((i * 8) + 2, new float3(1, 2, 3));
                hashMap.AddOrSet((i * 8) + 3, new Large());
                hashMap.AddOrSet((i * 8) + 4, new float3(3, 2, 1));
                hashMap.AddOrSet((i * 8) + 5, new Large());
                hashMap.AddOrSet((i * 8) + 6, 1);
                hashMap.AddOrSet((i * 8) + 7, (short)2);
            }

            for (var i = 0; i < 50; i++)
            {
                hashMap.AddOrSet((i * 8) + 0, 1);
                hashMap.AddOrSet((i * 8) + 1, (short)2);
                hashMap.AddOrSet((i * 8) + 2, new float3(1, 2, 3));
                hashMap.AddOrSet((i * 8) + 3, new Large());
                hashMap.AddOrSet((i * 8) + 4, new float3(3, 2, 1));
                hashMap.AddOrSet((i * 8) + 5, new Large());
                hashMap.AddOrSet((i * 8) + 6, 1);
                hashMap.AddOrSet((i * 8) + 7, (short)2);
            }
        }

        [Test]
        public void StressGet()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            for (var i = 0; i < 50; i++)
            {
                hashMap.GetOrAddRef((i * 8) + 0, 1);
                hashMap.GetOrAddRef((i * 8) + 1, (short)2);
                hashMap.GetOrAddRef((i * 8) + 2, new float3(1, 2, 3));
                hashMap.GetOrAddRef((i * 8) + 3, new Large());
                hashMap.GetOrAddRef((i * 8) + 4, new float3(3, 2, 1));
                hashMap.GetOrAddRef((i * 8) + 5, new Large());
                hashMap.GetOrAddRef((i * 8) + 6, 1);
                hashMap.GetOrAddRef((i * 8) + 7, (short)2);
            }

            for (var i = 0; i < 50; i++)
            {
                hashMap.GetOrAddRef((i * 8) + 0, 1);
                hashMap.GetOrAddRef((i * 8) + 1, (short)2);
                hashMap.GetOrAddRef((i * 8) + 2, new float3(1, 2, 3));
                hashMap.GetOrAddRef((i * 8) + 3, new Large());
                hashMap.GetOrAddRef((i * 8) + 4, new float3(3, 2, 1));
                hashMap.GetOrAddRef((i * 8) + 5, new Large());
                hashMap.GetOrAddRef((i * 8) + 6, 1);
                hashMap.GetOrAddRef((i * 8) + 7, (short)2);
            }
        }

        [Test]
        public void TryGetValue_WhenMissing_ReturnsFalseAndDefault()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            Assert.IsFalse(hashMap.TryGetValue(123, out int value));
            Assert.AreEqual(0, value);

            Assert.IsFalse(hashMap.TryGetValue(123, out Large large));
            Assert.AreEqual(default(Large), large);
        }

        [Test]
        public void TryGetValue_LargeValueOffsetBeyond255_Works()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            // Large is 16 bytes = 4 ints, so after 64 inserts the DataAllocatedIndex exceeds 255 and will have non-trivial high bytes.
            const int count = 70;
            for (var i = 0; i < count; i++)
            {
                hashMap.AddOrSet(i, new Large { TestValue0 = (ulong)(1000 + i), TestValue1 = (ulong)(2000 + i) });
            }

            Assert.IsTrue(hashMap.TryGetValue(count - 1, out Large last));
            Assert.AreEqual((ulong)(1000 + (count - 1)), last.TestValue0);
            Assert.AreEqual((ulong)(2000 + (count - 1)), last.TestValue1);

            Assert.IsTrue(hashMap.TryGetValue(64, out Large boundary));
            Assert.AreEqual(1064UL, boundary.TestValue0);
            Assert.AreEqual(2064UL, boundary.TestValue1);
        }

        [Test]
        public void AddOrSet_LargeValue_OverwritesExisting()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            const int key = 10;
            hashMap.AddOrSet(key, new Large { TestValue0 = 1, TestValue1 = 2 });
            hashMap.AddOrSet(key, new Large { TestValue0 = 3, TestValue1 = 4 });

            Assert.IsTrue(hashMap.TryGetValue(key, out Large value));
            Assert.AreEqual(3UL, value.TestValue0);
            Assert.AreEqual(4UL, value.TestValue1);
        }

        [Test]
        public void GetOrAddRef_LargeValue_AllowsMutationByRef()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            const int key = 42;

            ref var value = ref hashMap.GetOrAddRef(key, default(Large));
            value.TestValue0 = 123;
            value.TestValue1 = 456;

            Assert.IsTrue(hashMap.TryGetValue(key, out Large stored));
            Assert.AreEqual(123UL, stored.TestValue0);
            Assert.AreEqual(456UL, stored.TestValue1);
        }

        [Test]
        public void ResizeData_WhenCapacityExceedsDataCapacity_DoesNotCorrupt()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            // Start tiny so Capacity grows via Resize while DataCapacity remains at the original initialization size.
            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(1).AsUntypedHashMap<TestBuffer, int>();

            for (var i = 0; i < 128; i++)
            {
                hashMap.AddOrSet(i, i);
            }

            hashMap.AddOrSet(1000, new Large { TestValue0 = 111, TestValue1 = 222 });
            hashMap.AddOrSet(1001, new Large { TestValue0 = 333, TestValue1 = 444 });

            Assert.IsTrue(hashMap.TryGetValue(0, out int first));
            Assert.AreEqual(0, first);

            Assert.IsTrue(hashMap.TryGetValue(127, out int last));
            Assert.AreEqual(127, last);

            Assert.IsTrue(hashMap.TryGetValue(1000, out Large large0));
            Assert.AreEqual(111UL, large0.TestValue0);
            Assert.AreEqual(222UL, large0.TestValue1);

            Assert.IsTrue(hashMap.TryGetValue(1001, out Large large1));
            Assert.AreEqual(333UL, large1.TestValue0);
            Assert.AreEqual(444UL, large1.TestValue1);
        }

        [Test]
        public unsafe void KeysPointer_IsAlignedToKeyType_LongKey_DefaultInit()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBufferLongKey));
            var buffer = this.Manager.GetBuffer<TestBufferLongKey>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBufferLongKey, long>().AsUntypedHashMap<TestBufferLongKey, long>();

            var helper = hashMap.Helper;
            var align = UnsafeUtility.AlignOf<long>();
            var keysPtr = (ulong)helper->Keys;
            Assert.AreEqual(0u, keysPtr % (ulong)align);
        }

        [Test]
        public unsafe void LargeValuePointer_IsAligned_WhenMixedLargeTypes()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            // float3 is 12 bytes (3 ints). Large is 16 bytes and requires 8-byte alignment.
            // Without alignment, the Large allocation can start at an odd int index and become misaligned.
            hashMap.AddOrSet(1, new float3(1, 2, 3));
            hashMap.AddOrSet(2, new Large { TestValue0 = 10, TestValue1 = 20 });

            var helper = hashMap.Helper;
            var idx = helper->Find(2);
            Assert.AreNotEqual(-1, idx);

            var offset = *((int*)helper->Values + idx);
            var ptr = (byte*)(helper->Data + offset);

            var align = UnsafeUtility.AlignOf<Large>();
            Assert.AreEqual(0u, ((ulong)ptr) % (ulong)align);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public void GetOrAddRef_WhenTypeDoesNotMatch_Throws()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>().AsUntypedHashMap<TestBuffer, int>();

            hashMap.AddOrSet(0, 1);

            Assert.Throws<InvalidOperationException>(() =>
            {
                hashMap.GetOrAddRef(0, (short)0);
            });
        }
#endif

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IDynamicUntypedHashMap<int>
        {
            byte IDynamicUntypedHashMap<int>.Value { get; }
        }

        [InternalBufferCapacity(0)]
        private struct TestBufferLongKey : IDynamicUntypedHashMap<long>
        {
            byte IDynamicUntypedHashMap<long>.Value { get; }
        }

        public struct Large
        {
            public ulong TestValue0;
            public ulong TestValue1;
        }
    }
}
