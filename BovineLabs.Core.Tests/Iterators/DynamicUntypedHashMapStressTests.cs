// <copyright file="DynamicUntypedHashMapStressTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;
    using Random = Unity.Mathematics.Random;

    public class DynamicUntypedHashMapStressTests : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            TypeManagerEx.Initialize();
        }

        [Test]
        public void Stress_MixedTypes_ManyKeys_PreserveValuesAcrossResizes()
        {
            const int count = 4096;

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            // Start extremely small to maximize the number of resizes.
            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(1).AsUntypedHashMap<TestBuffer, int>();

            var expectedInt = new Dictionary<int, int>(count);
            var expectedShort = new Dictionary<int, short>(count);
            var expectedFloat3 = new Dictionary<int, float3>(count);
            var expectedLarge = new Dictionary<int, Large>(count);

            for (var i = 0; i < count; i++)
            {
                var key = i;
                var kind = i & 3;

                switch (kind)
                {
                    case 0:
                        var intValue = (i * 7919) ^ 0x5A5A5A5A;
                        hashMap.AddOrSet(key, intValue);
                        expectedInt[key] = intValue;
                        break;
                    case 1:
                        var shortValue = (short)((i * 97) ^ 0x1234);
                        hashMap.AddOrSet(key, shortValue);
                        expectedShort[key] = shortValue;
                        break;
                    case 2:
                        var floatValue = new float3(i, i * 2, i * 3);
                        hashMap.AddOrSet(key, floatValue);
                        expectedFloat3[key] = floatValue;
                        break;
                    default:
                        var largeValue = new Large { TestValue0 = (ulong)(1000000 + i), TestValue1 = (ulong)(2000000 + i) };
                        hashMap.AddOrSet(key, largeValue);
                        expectedLarge[key] = largeValue;
                        break;
                }
            }

            Assert.AreEqual(count, hashMap.Count);

            foreach (var kv in expectedInt)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out int actual));
                Assert.AreEqual(kv.Value, actual);
            }

            foreach (var kv in expectedShort)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out short actual));
                Assert.AreEqual(kv.Value, actual);
            }

            foreach (var kv in expectedFloat3)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out float3 actual));
                Assert.AreEqual(kv.Value, actual);
            }

            foreach (var kv in expectedLarge)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out Large actual));
                Assert.AreEqual(kv.Value.TestValue0, actual.TestValue0);
                Assert.AreEqual(kv.Value.TestValue1, actual.TestValue1);
            }
        }

        [Test]
        public unsafe void Stress_MixedLargeTypes_AlignmentIsMaintainedAcrossResizes()
        {
            const int count = 2048;

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            // Start tiny so both the map and the Data segment will be resized repeatedly.
            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(1).AsUntypedHashMap<TestBuffer, int>();

            // Insert alternating large types: float3 (3 ints) then Large (4 ints) so the allocation index would misalign without alignment.
            for (var i = 0; i < count; i++)
            {
                var key = i;
                if ((i & 1) == 0)
                {
                    hashMap.AddOrSet(key, new float3(i, i + 1, i + 2));
                }
                else
                {
                    hashMap.AddOrSet(key, new Large { TestValue0 = (ulong)(10 + i), TestValue1 = (ulong)(20 + i) });
                }

                // Periodically force a resize using the public API to exercise the full rehash/copy path too.
                if ((i & 255) == 255)
                {
                    hashMap.Capacity = hashMap.Capacity * 2;
                }
            }

            var helper = hashMap.Helper;

            // Validate alignment for every Large entry.
            var largeAlign = UnsafeUtility.AlignOf<Large>();
            for (var i = 1; i < count; i += 2)
            {
                var idx = helper->Find(i);
                Assert.AreNotEqual(-1, idx);

                var offset = *((int*)helper->Values + idx);
                var ptr = (byte*)(helper->Data + offset);
                Assert.AreEqual(0u, ((ulong)ptr) % (ulong)largeAlign);

                Assert.IsTrue(hashMap.TryGetValue(i, out Large stored));
                Assert.AreEqual((ulong)(10 + i), stored.TestValue0);
                Assert.AreEqual((ulong)(20 + i), stored.TestValue1);
            }
        }

        [Test]
        public unsafe void Stress_OverwriteExistingLarge_DoesNotReallocateOrCorrupt()
        {
            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(1).AsUntypedHashMap<TestBuffer, int>();

            const int key = 12345;

            hashMap.AddOrSet(key, new Large { TestValue0 = 1, TestValue1 = 2 });

            var helper = hashMap.Helper;
            var idx = helper->Find(key);
            Assert.AreNotEqual(-1, idx);

            var storedOffset = *((int*)helper->Values + idx);
            var dataAllocatedBefore = helper->DataAllocatedIndex;

            for (var i = 0; i < 2048; i++)
            {
                hashMap.AddOrSet(key, new Large { TestValue0 = (ulong)(100 + i), TestValue1 = (ulong)(200 + i) });

                if ((i & 255) == 255)
                {
                    hashMap.Capacity = hashMap.Capacity * 2;
                }
            }

            helper = hashMap.Helper;
            idx = helper->Find(key);
            Assert.AreNotEqual(-1, idx);

            // The offset must remain the same, and DataAllocatedIndex should not advance when overwriting existing entries.
            Assert.AreEqual(storedOffset, *((int*)helper->Values + idx));
            Assert.AreEqual(dataAllocatedBefore, helper->DataAllocatedIndex);

            Assert.IsTrue(hashMap.TryGetValue(key, out Large finalValue));
            Assert.AreEqual((ulong)(100 + 2047), finalValue.TestValue0);
            Assert.AreEqual((ulong)(200 + 2047), finalValue.TestValue1);
        }

        [Test]
        public void Stress_Fuzz_AddOrSetAndGetOrAddRef_ValidOperations()
        {
            const int operations = 5000;
            const int keySpace = 2048;

            var entity = this.Manager.CreateEntity(typeof(TestBuffer));
            var buffer = this.Manager.GetBuffer<TestBuffer>(entity);

            var hashMap = buffer.InitializeUntypedHashMap<TestBuffer, int>(1).AsUntypedHashMap<TestBuffer, int>();

            var expectedInt = new Dictionary<int, int>();
            var expectedFloat3 = new Dictionary<int, float3>();
            var expectedKind = new Dictionary<int, byte>();

            var random = new Random(12345);
            for (var i = 0; i < operations; i++)
            {
                var key = random.NextInt(0, keySpace);
                var doInt = (random.NextInt() & 1) == 0;

                // DynamicUntypedHashMap stores a single type per key; once set, the key's type cannot change.
                // To stress valid behavior, pick a type the first time a key is seen and keep it stable.
                if (expectedKind.TryGetValue(key, out var kind))
                {
                    doInt = kind == 0;
                }
                else
                {
                    expectedKind.Add(key, (byte)(doInt ? 0 : 1));
                }

                if (doInt)
                {
                    var value = random.NextInt();
                    if ((random.NextInt() & 1) == 0)
                    {
                        hashMap.AddOrSet(key, value);
                    }
                    else
                    {
                        ref var r = ref hashMap.GetOrAddRef(key, value);
                        r = value;
                    }

                    expectedInt[key] = value;
                    expectedFloat3.Remove(key);
                }
                else
                {
                    var value = new float3(random.NextInt(0, 1000), random.NextInt(0, 1000), random.NextInt(0, 1000));
                    if ((random.NextInt() & 1) == 0)
                    {
                        hashMap.AddOrSet(key, value);
                    }
                    else
                    {
                        ref var r = ref hashMap.GetOrAddRef(key, value);
                        r = value;
                    }

                    expectedFloat3[key] = value;
                    expectedInt.Remove(key);
                }

                if ((i & 511) == 511)
                {
                    hashMap.Capacity = hashMap.Capacity * 2;
                }
            }

            foreach (var kv in expectedInt)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out int actual));
                Assert.AreEqual(kv.Value, actual);
            }

            foreach (var kv in expectedFloat3)
            {
                Assert.IsTrue(hashMap.TryGetValue(kv.Key, out float3 actual));
                Assert.AreEqual(kv.Value, actual);
            }
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IDynamicUntypedHashMap<int>
        {
            byte IDynamicUntypedHashMap<int>.Value { get; }
        }

        public struct Large
        {
            public ulong TestValue0;
            public ulong TestValue1;
        }
    }
}
