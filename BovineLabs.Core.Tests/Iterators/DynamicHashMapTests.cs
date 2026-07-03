// <copyright file="DynamicHashMapTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public class DynamicHashMapTests : ECSTestsFixture
    {
        private const int MinGrowth = 64;

        [Test]
        public void Capacity()
        {
            const int newCapacity = 128;

            var hashMap = this.CreateHashMap();
            Assert.AreEqual(MinGrowth, hashMap.Capacity);

            hashMap.Capacity = newCapacity;

            Assert.AreEqual(newCapacity, hashMap.Capacity);
        }

        [Test]
        public void AddRemove()
        {
            const int count = 1024;

            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.TryAdd(i + i, (byte)i));
            }

            Assert.AreEqual(count, hashMap.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.Remove(i + i));
            }

            Assert.AreEqual(0, hashMap.Count);
        }

        [Test]
        public void AddBatchUnsafe()
        {
            const int count = 1027;

            var hashMap = this.CreateHashMap();

            var keys = new NativeArray<int>(count, Allocator.Temp);
            var values = new NativeArray<byte>(count, Allocator.Temp);

            for (var i = 0; i < count; i++)
            {
                keys[i] = i;
                values[i] = (byte)(i % byte.MaxValue);
            }

            hashMap.AddBatchUnsafe(keys, values);

            Assert.AreEqual(count, hashMap.Count);

            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.ContainsKey(i));
            }
        }

        [Test]
        public void TryGetValue()
        {
            var hashMap = this.CreateHashMap();
            Assert.IsFalse(hashMap.TryGetValue(47, out _));

            hashMap.Add(47, 123);
            Assert.IsTrue(hashMap.TryGetValue(47, out var result));
            Assert.AreEqual(123, result);

            hashMap.Remove(47);
            Assert.IsFalse(hashMap.TryGetValue(47, out _));
        }

        [Test]
        public void IndexerSetExisting()
        {
            var hashMap = this.CreateHashMap();

            // Add initial value
            hashMap.Add(42, 50);
            Assert.AreEqual(50, hashMap[42]);

            // Test indexer setter on existing key (tests optimized path)
            hashMap[42] = 75;
            Assert.AreEqual(75, hashMap[42]);
            Assert.AreEqual(1, hashMap.Count); // Should still be 1 element
        }

        [Test]
        public void IndexerSetNew()
        {
            var hashMap = this.CreateHashMap();

            // Test indexer setter on new key (tests optimized path)
            hashMap[42] = 100;
            Assert.AreEqual(100, hashMap[42]);
            Assert.AreEqual(1, hashMap.Count);
        }

        [Test]
        public void GetOrAddRef()
        {
            var hashMap = this.CreateHashMap();

            // Test with new key
            ref var value1 = ref hashMap.GetOrAddRefUnsafe(42, 50);
            Assert.AreEqual(50, value1);
            Assert.AreEqual(1, hashMap.Count);

            // Modify through reference
            value1 = 75;
            Assert.AreEqual(75, hashMap[42]);

            // Test with existing key
            ref var value2 = ref hashMap.GetOrAddRefUnsafe(42, 125);
            Assert.AreEqual(75, value2); // Should return existing value, not default
            Assert.AreEqual(1, hashMap.Count); // Still only one element
        }

        [Test]
        public void GetOrAddRefWithFlag()
        {
            var hashMap = this.CreateHashMap();

            // Test with new key
            ref var value1 = ref hashMap.GetOrAddRefUnsafe(42, out var wasAdded1, 50);
            Assert.IsTrue(wasAdded1);
            Assert.AreEqual(50, value1);

            // Test with existing key
            ref var value2 = ref hashMap.GetOrAddRefUnsafe(42, out var wasAdded2, 125);
            Assert.IsFalse(wasAdded2);
            Assert.AreEqual(50, value2); // Should return existing value
        }

        [Test]
        public void EnumerationConsistency()
        {
            const int count = 100;
            var hashMap = this.CreateHashMap();

            // Add elements
            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i % 255));
            }

            // Test enumeration gives all elements
            var found = new HashSet<int>();
            foreach (var kvp in hashMap)
            {
                Assert.IsFalse(found.Contains(kvp.Key), $"Duplicate key {kvp.Key} found during enumeration");
                found.Add(kvp.Key);
                Assert.AreEqual((byte)(kvp.Key % 255), kvp.Value);
            }

            Assert.AreEqual(count, found.Count);
        }

        [Test]
        public void ResizeStressTest()
        {
            var hashMap = this.CreateHashMap();

            // Force multiple resizes by adding many elements
            const int count = 1000;
            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i % 255));
            }

            // Verify all elements are still present and correct
            for (var i = 0; i < count; i++)
            {
                Assert.IsTrue(hashMap.TryGetValue(i, out var value), $"Key {i} not found after resize");
                Assert.AreEqual((byte)(i % 255), value, $"Incorrect value for key {i} after resize");
            }

            Assert.AreEqual(count, hashMap.Count);
        }

        [Test]
        public unsafe void ValuesPointer_IsAlignedToValueType()
        {
            var hashMap = this.CreateHashMapLong();

            var align = UnsafeUtility.AlignOf<long>();
            var valuesPtr = (ulong)hashMap.Helper->Values;
            Assert.AreEqual(0u, valuesPtr % (ulong)align);
        }

        [Test]
        public unsafe void Resize_WithHoles_RebuildsAndClearsFreeList()
        {
            const int count = 256;
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)i);
            }

            // Create holes.
            for (var i = 0; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.Remove(i));
            }

            Assert.AreEqual(count / 2, hashMap.Count);

            // Force a resize while the map has holes.
            hashMap.Capacity *= 2;

            var helper = hashMap.Helper;
            Assert.AreEqual(-1, helper->FirstFreeIdx);
            Assert.AreEqual(helper->Count, helper->AllocatedIndex);

            for (var i = 1; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.TryGetValue(i, out var value));
                Assert.AreEqual((byte)i, value);
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsFalse(hashMap.TryGetValue(i, out _));
            }
        }

        [Test]
        public unsafe void CopyActiveEntriesTo_WithDenseMap_UsesActiveEntries()
        {
            const int count = 32;
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i + 1));
            }

            Assert.IsTrue(hashMap.Helper->IsDense);

            var keys = new NativeArray<int>(count, Allocator.Temp);
            var values = new NativeArray<byte>(count, Allocator.Temp);

            var copied = hashMap.Helper->CopyActiveEntriesTo((byte*)keys.GetUnsafePtr(), sizeof(int), (byte*)values.GetUnsafePtr(), sizeof(byte));

            Assert.AreEqual(count, copied);

            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual(i, keys[i]);
                Assert.AreEqual((byte)(i + 1), values[i]);
            }
        }

        [Test]
        public unsafe void CopyActiveEntriesTo_WithSparseMap_SkipsRemovedEntriesWithoutMutation()
        {
            const int count = 64;
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                hashMap.Add(i, (byte)(i + 1));
            }

            for (var i = 0; i < count; i += 2)
            {
                Assert.IsTrue(hashMap.Remove(i));
            }

            Assert.IsFalse(hashMap.Helper->IsDense);

            var helper = hashMap.Helper;
            var firstFreeIndex = helper->FirstFreeIdx;
            var allocatedIndex = helper->AllocatedIndex;
            var activeCount = hashMap.Count;
            var keys = new NativeArray<int>(activeCount, Allocator.Temp);
            var values = new NativeArray<byte>(activeCount, Allocator.Temp);

            var copied = helper->CopyActiveEntriesTo((byte*)keys.GetUnsafePtr(), sizeof(int), (byte*)values.GetUnsafePtr(), sizeof(byte));

            Assert.AreEqual(activeCount, copied);
            Assert.AreEqual(firstFreeIndex, helper->FirstFreeIdx);
            Assert.AreEqual(allocatedIndex, helper->AllocatedIndex);
            Assert.AreEqual(activeCount, helper->Count);

            var found = new HashSet<int>();
            for (var i = 0; i < copied; i++)
            {
                Assert.IsTrue((keys[i] & 1) == 1, $"Removed key {keys[i]} was packed.");
                Assert.AreEqual((byte)(keys[i] + 1), values[i]);
                found.Add(keys[i]);
            }

            Assert.AreEqual(activeCount, found.Count);
        }

        [Test]
        public unsafe void DenseRebuild_RoundTripsPackedSparseMap()
        {
            const int count = 96;
            var source = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                source.Add(i, (byte)(i + 7));
            }

            for (var i = 0; i < count; i += 3)
            {
                Assert.IsTrue(source.Remove(i));
            }

            var sourceHelper = source.Helper;
            var packedCount = source.Count;
            var keys = new NativeArray<int>(packedCount, Allocator.Temp);
            var values = new NativeArray<byte>(packedCount, Allocator.Temp);
            sourceHelper->CopyActiveEntriesTo((byte*)keys.GetUnsafePtr(), sizeof(int), (byte*)values.GetUnsafePtr(), sizeof(byte));

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            var targetSize = DynamicHashMapHelper<int>.CalculateDataSize(sourceHelper->Capacity, sizeof(byte), out _);
            targetBytes.ResizeUninitialized(targetSize);

            var writeView = DynamicHashMapHelper<int>.BeginDenseRebuild(
                targetBytes.GetPtr(), targetBytes.Length, packedCount, sourceHelper->Capacity, sizeof(byte), sourceHelper->Log2MinGrowth);

            UnsafeUtility.MemCpy(writeView.Keys, keys.GetUnsafeReadOnlyPtr(), packedCount * sizeof(int));
            UnsafeUtility.MemCpy(writeView.Values, values.GetUnsafeReadOnlyPtr(), packedCount * sizeof(byte));
            DynamicHashMapHelper<int>.CompleteDenseRebuild(ref writeView);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            Assert.AreEqual(packedCount, rebuilt.Count);
            Assert.AreEqual(sourceHelper->Capacity, rebuilt.Capacity);
            Assert.IsTrue(rebuilt.Helper->IsDense);
            Assert.AreEqual(-1, rebuilt.Helper->FirstFreeIdx);
            Assert.AreEqual(packedCount, rebuilt.Helper->AllocatedIndex);

            for (var i = 0; i < count; i++)
            {
                var shouldExist = i % 3 != 0;
                Assert.AreEqual(shouldExist, rebuilt.TryGetValue(i, out var value), $"Unexpected presence for key {i}.");

                if (shouldExist)
                {
                    Assert.AreEqual((byte)(i + 7), value);
                }
            }
        }

        [Test]
        public unsafe void CompactHeader_UsesExplicitByteLayout()
        {
            var header = DynamicHashMapCompactHeader.Create(0x01020304, 0x05060708, 0x090a0b0c, 13);
            var bytes = new NativeArray<byte>(DynamicHashMapCompactHeader.Size, Allocator.Temp);

            header.Write((byte*)bytes.GetUnsafePtr());

            Assert.AreEqual(0x04, bytes[0]);
            Assert.AreEqual(0x03, bytes[1]);
            Assert.AreEqual(0x02, bytes[2]);
            Assert.AreEqual(0x01, bytes[3]);
            Assert.AreEqual(0x08, bytes[4]);
            Assert.AreEqual(0x07, bytes[5]);
            Assert.AreEqual(0x06, bytes[6]);
            Assert.AreEqual(0x05, bytes[7]);
            Assert.AreEqual(0x0c, bytes[8]);
            Assert.AreEqual(0x0b, bytes[9]);
            Assert.AreEqual(0x0a, bytes[10]);
            Assert.AreEqual(0x09, bytes[11]);
            Assert.AreEqual(13, bytes[12]);
            Assert.AreEqual(DynamicHashMapCompactHeader.CurrentFormatVersion, bytes[13]);
            Assert.AreEqual(0, bytes[14]);
            Assert.AreEqual(0, bytes[15]);

            var read = DynamicHashMapCompactHeader.Read((byte*)bytes.GetUnsafeReadOnlyPtr());
            Assert.AreEqual(header.Count, read.Count);
            Assert.AreEqual(header.Capacity, read.Capacity);
            Assert.AreEqual(header.PayloadBytes, read.PayloadBytes);
            Assert.AreEqual(header.Log2MinGrowth, read.Log2MinGrowth);
            Assert.AreEqual(header.FormatVersion, read.FormatVersion);
            Assert.AreEqual(header.Flags, read.Flags);
        }

        [Test]
        public unsafe void RawCompactPayload_WithDenseMap_RoundTripsActiveEntries()
        {
            const int count = 40;
            var source = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                source.Add(i, (byte)(i + 11));
            }

            var payloadBytes = DynamicHashMapRawCompactPayload<int, byte>.CalculatePayloadBytes(source.Count);
            var payload = new NativeArray<byte>(payloadBytes, Allocator.Temp);

            var header = DynamicHashMapRawCompactPayload<int, byte>.Pack(source.Helper, (byte*)payload.GetUnsafePtr(), payload.Length);

            Assert.AreEqual(source.Count, header.Count);
            Assert.AreEqual(source.Capacity, header.Capacity);
            Assert.AreEqual(payloadBytes, header.PayloadBytes);
            Assert.AreEqual(DynamicHashMapCompactHeader.Size + (source.Count * (sizeof(int) + sizeof(byte))), header.PayloadBytes);
            Assert.IsTrue(DynamicHashMapRawCompactPayload<int, byte>.TryGetTargetDataSize(
                (byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length, out var targetSize));

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(targetSize);
            DynamicHashMapRawCompactPayload<int, byte>.RebuildFromPayload(
                targetBytes.GetPtr(), targetBytes.Length, (byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            AssertCompactRoundTrip(source, rebuilt);
        }

        [Test]
        public unsafe void RawCompactPayload_WithSparseMap_RoundTripsWithoutMutation()
        {
            const int count = 96;
            var source = this.CreateHashMap();

            for (var i = 0; i < count; i++)
            {
                source.Add(i, (byte)(i + 17));
            }

            for (var i = 0; i < count; i += 3)
            {
                Assert.IsTrue(source.Remove(i));
            }

            var helper = source.Helper;
            var firstFreeIndex = helper->FirstFreeIdx;
            var allocatedIndex = helper->AllocatedIndex;
            var payloadBytes = DynamicHashMapRawCompactPayload<int, byte>.CalculatePayloadBytes(source.Count);
            var payload = new NativeArray<byte>(payloadBytes, Allocator.Temp);

            DynamicHashMapRawCompactPayload<int, byte>.Pack(helper, (byte*)payload.GetUnsafePtr(), payload.Length);

            Assert.AreEqual(firstFreeIndex, helper->FirstFreeIdx);
            Assert.AreEqual(allocatedIndex, helper->AllocatedIndex);
            Assert.AreEqual(count - (count / 3), helper->Count);
            Assert.IsTrue(DynamicHashMapRawCompactPayload<int, byte>.TryGetTargetDataSize(
                (byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length, out var targetSize));

            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            targetBytes.ResizeUninitialized(targetSize);
            DynamicHashMapRawCompactPayload<int, byte>.RebuildFromPayload(
                targetBytes.GetPtr(), targetBytes.Length, (byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length);

            var rebuilt = targetBuffer.AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
            AssertCompactRoundTrip(source, rebuilt);
        }

        [Test]
        public unsafe void RawCompactPayload_WithMalformedHeader_IsRejected()
        {
            var payload = new NativeArray<byte>(DynamicHashMapCompactHeader.Size, Allocator.Temp);
            var header = new DynamicHashMapCompactHeader
            {
                Count = 2,
                Capacity = 1,
                PayloadBytes = DynamicHashMapCompactHeader.Size,
                Log2MinGrowth = 6,
                FormatVersion = DynamicHashMapCompactHeader.CurrentFormatVersion,
                Flags = DynamicHashMapCompactHeader.CurrentFlags,
            };

            header.Write((byte*)payload.GetUnsafePtr());

            Assert.IsTrue(DynamicHashMapRawCompactPayload<int, byte>.TryReadHeader((byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length, out var read));
            Assert.IsFalse(DynamicHashMapRawCompactPayload<int, byte>.TryValidateHeader(read, payload.Length));

            header = DynamicHashMapCompactHeader.Create(0, MinGrowth, DynamicHashMapCompactHeader.Size, 6);
            header.FormatVersion++;
            header.Write((byte*)payload.GetUnsafePtr());

            Assert.IsTrue(DynamicHashMapRawCompactPayload<int, byte>.TryReadHeader((byte*)payload.GetUnsafeReadOnlyPtr(), payload.Length, out read));
            Assert.IsFalse(DynamicHashMapRawCompactPayload<int, byte>.TryValidateHeader(read, payload.Length));
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public unsafe void DenseRebuild_WithDuplicateKeys_Throws()
        {
            var targetBuffer = this.CreateHashMapBuffer();
            var targetBytes = targetBuffer.Reinterpret<byte>();
            var targetSize = DynamicHashMapHelper<int>.CalculateDataSize(MinGrowth, sizeof(byte), out _);
            targetBytes.ResizeUninitialized(targetSize);

            var writeView = DynamicHashMapHelper<int>.BeginDenseRebuild(targetBytes.GetPtr(), targetBytes.Length, 2, MinGrowth, sizeof(byte), 6);

            writeView.Keys[0] = 17;
            writeView.Keys[1] = 17;
            writeView.Values[0] = 1;
            writeView.Values[1] = 2;

            Assert.Throws<ArgumentException>(() => DynamicHashMapHelper<int>.CompleteDenseRebuild(ref writeView));
        }

        [Test]
        public void AddBatchUnsafe_WithHoles_Throws()
        {
            var hashMap = this.CreateHashMap();

            for (var i = 0; i < 32; i++)
            {
                hashMap.Add(i, (byte)i);
            }

            // Create holes.
            for (var i = 0; i < 32; i += 2)
            {
                Assert.IsTrue(hashMap.Remove(i));
            }

            var keys = new NativeArray<int>(2, Allocator.Temp);
            var values = new NativeArray<byte>(2, Allocator.Temp);
            keys[0] = 100;
            keys[1] = 101;
            values[0] = 1;
            values[1] = 2;

            Assert.Throws<InvalidOperationException>(() => hashMap.AddBatchUnsafe(keys, values));
        }
#endif

        private DynamicHashMap<int, byte> CreateHashMap()
        {
            return this.CreateHashMapBuffer().AsHashMap<DynamicHashMapTestsBuffer, int, byte>();
        }

        private DynamicBuffer<DynamicHashMapTestsBuffer> CreateHashMapBuffer()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicHashMapTestsBuffer));
            return this.Manager.GetBuffer<DynamicHashMapTestsBuffer>(entity).InitializeHashMap<DynamicHashMapTestsBuffer, int, byte>(0, MinGrowth);
        }

        private DynamicHashMap<int, long> CreateHashMapLong()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicHashMapTestsLongBuffer));
            return this.Manager.GetBuffer<DynamicHashMapTestsLongBuffer>(entity)
                .InitializeHashMap<DynamicHashMapTestsLongBuffer, int, long>(0, MinGrowth)
                .AsHashMap<DynamicHashMapTestsLongBuffer, int, long>();
        }

        private static unsafe void AssertCompactRoundTrip(DynamicHashMap<int, byte> source, DynamicHashMap<int, byte> rebuilt)
        {
            Assert.AreEqual(source.Count, rebuilt.Count);
            Assert.AreEqual(source.Capacity, rebuilt.Capacity);
            Assert.IsTrue(rebuilt.Helper->IsDense);
            Assert.AreEqual(-1, rebuilt.Helper->FirstFreeIdx);
            Assert.AreEqual(rebuilt.Count, rebuilt.Helper->AllocatedIndex);

            foreach (var pair in source)
            {
                Assert.IsTrue(rebuilt.TryGetValue(pair.Key, out var value), $"Missing key {pair.Key} after compact round-trip.");
                Assert.AreEqual(pair.Value, value);
            }
        }
    }
}
