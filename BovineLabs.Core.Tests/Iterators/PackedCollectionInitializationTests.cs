// <copyright file="PackedCollectionInitializationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using OneHash = BovineLabs.Core.Iterators.DynamicVariableMapHelper<int, long, byte, BovineLabs.Core.Iterators.Columns.MultiHashColumn<byte>>;
    using OneOrdered = BovineLabs.Core.Iterators.DynamicVariableMapHelper<int, long, byte, BovineLabs.Core.Iterators.Columns.OrderedListColumn<byte>>;
    using TwoColumns = BovineLabs.Core.Iterators.DynamicVariableMapHelper<int, long, byte, BovineLabs.Core.Iterators.Columns.MultiHashColumn<byte>, short, BovineLabs.Core.Iterators.Columns.OrderedListColumn<short>>;

    public unsafe class PackedCollectionInitializationTests : ECSTestsFixture
    {
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        [TestCase(4, 1)]
        [TestCase(4, 4)]
        public void RegularMap_InitAndFill_InitializeReserveAndGaps(int capacity, int count)
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, capacity, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                for (var i = 0; i < count; ++i)
                {
                    AddRegular(bytes, ref data, i + 1, (byte)(i + 20));
                }

                Assert.AreEqual(count, data->Count);
                for (var i = 0; i < count; ++i)
                {
                    Assert.IsTrue(data->TryGetValue(i + 1, out byte value));
                    Assert.AreEqual((byte)(i + 20), value);
                }

                AssertRegularStorage(data);
            });
        }

        [Test]
        public void RegularMap_RemoveAndResize_PreserveLiveIndicesAboveCount()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 4, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                AddRegular(bytes, ref data, 1, 21);
                AddRegular(bytes, ref data, 2, 22);
                AddRegular(bytes, ref data, 3, 23);
                Assert.AreEqual(1, data->TryRemove(1));
                Assert.AreEqual(2, data->Find(3));
                Assert.AreEqual(2, data->Count);
                AssertRegularStorage(data);
                Assert.IsTrue(data->TryGetValue(3, out byte survivor));
                Assert.AreEqual(23, survivor);

                // Sparse growth rebuilds entries densely; it must not lose slot 2.
                DynamicHashMapHelper<int>.Resize(bytes, ref data, 9);
                Assert.IsTrue(data->IsDense);
                Assert.AreEqual(2, data->Count);
                Assert.IsTrue(data->TryGetValue(2, out byte value2));
                Assert.IsTrue(data->TryGetValue(3, out byte value3));
                Assert.AreEqual(22, value2);
                Assert.AreEqual(23, value3);
                AssertRegularStorage(data);

                // Same-layout flattening and shrinking must also clean reserve.
                DynamicHashMapHelper<int>.Flatten(bytes, ref data);
                DynamicHashMapHelper<int>.ResizeExact(bytes, ref data, 2, 4);
                Assert.IsTrue(data->TryGetValue(3, out value3));
                Assert.AreEqual(23, value3);
                AssertRegularStorage(data);
            });
        }

        [Test]
        public void RegularMap_DenseGrowthThenShiftDown_ClearsVacatedSuffix()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 1, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                for (var i = 0; i < 12; ++i)
                {
                    AddRegular(bytes, ref data, i + 1, (byte)(i + 21));
                }

                AssertRegularStorage(data);
                data->RemoveRangeShiftDown(2, 3);
                Assert.AreEqual(9, data->Count);
                Assert.AreEqual(-1, data->Find(3));
                Assert.AreEqual(-1, data->Find(4));
                Assert.AreEqual(-1, data->Find(5));
                Assert.IsTrue(data->TryGetValue(12, out byte last));
                Assert.AreEqual(32, last);
                AssertRegularStorage(data);
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RegularMap_ClearAndReuse_ResetsPayloadAndChains(bool dense)
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 4, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                AddRegular(bytes, ref data, 1, 21);
                AddRegular(bytes, ref data, 9, 29); // Collides with 1 at capacity 4.
                if (dense)
                {
                    data->ClearDense();
                }
                else
                {
                    data->Clear();
                }

                Assert.AreEqual(0, data->Count);
                AssertRegularStorage(data);
                for (var i = 0; i < data->Capacity; ++i)
                {
                    Assert.AreEqual(-1, data->Next[i]);
                }

                AddRegular(bytes, ref data, 3, 33);
                Assert.IsTrue(data->TryGetValue(3, out byte value));
                Assert.AreEqual(33, value);
                AssertRegularStorage(data);
            });
        }

        [Test]
        public void RegularMap_MultiRemoveAndIteratorRemove_ClearExactSlots()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 4, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                var a = DynamicHashMapHelper<int>.AddMulti(bytes, ref data, 1);
                data->Values[a] = 21;
                var b = DynamicHashMapHelper<int>.AddMulti(bytes, ref data, 1);
                data->Values[b] = 22;
                AddRegular(bytes, ref data, 9, 29);
                Assert.AreEqual(2, data->Remove(1));
                Assert.IsTrue(data->TryGetFirstValue(9, out byte value, out var iterator));
                Assert.AreEqual(29, value);
                AssertRegularStorage(data);
                data->Remove(iterator);
                Assert.AreEqual(0, data->Count);
                AssertRegularStorage(data);
                data->ClearDense(); // Sparse fallback.
                AssertRegularStorage(data);
            });
        }

        [Test]
        public void DenseRebuild_InitializesWholeLayoutAndDiscardsFinalUnusedPrefix()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                var size = DynamicHashMapHelper<int>.CalculateDataSize(3, sizeof(byte), out _);
                bytes.ResizeUninitialized(size);
                var view = DynamicHashMapHelper<int>.BeginDenseRebuild(bytes.GetUnsafePtr(), bytes.Length, 2, 3, sizeof(byte), 0);
                view.Keys[0] = 11;
                view.Keys[1] = 12;
                view.Values[0] = 41;
                view.Values[1] = 42;
                view.Count = 1;
                DynamicHashMapHelper<int>.CompleteDenseRebuild(ref view);
                var data = bytes.AsHelper<int>();
                Assert.AreEqual(1, data->Count);
                Assert.AreEqual(-1, data->Find(12));
                Assert.IsTrue(data->TryGetValue(11, out byte value));
                Assert.AreEqual(41, value);
                AssertRegularStorage(data);
            });
        }

        [Test]
        public void HashSet_InitializesUnusedKeysWithoutValueStorage()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 4, 0, 0);
                var data = bytes.AsHelper<int>();
                DynamicHashMapHelper<int>.AddUnique(bytes, ref data, 11);
                DynamicHashMapHelper<int>.Resize(bytes, ref data, 9);
                Assert.AreNotEqual(-1, data->Find(11));
                AssertRegularStorage(data);
                data->TryRemove(11);
                AssertRegularStorage(data);
            });
        }

        [Test]
        public void RegularMap_PhysicalReallocation_PreservesEntriesAndInitializesNewReserve()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicHashMapHelper<int>.Init(bytes, 1, sizeof(byte), 0);
                var data = bytes.AsHelper<int>();
                var originalCapacity = bytes.Capacity;
                for (var i = 0; i < 80; ++i)
                {
                    AddRegular(bytes, ref data, i + 1, (byte)(i + 20));
                }

                Assert.Greater(bytes.Length, originalCapacity, "Expected growth beyond the deliberately small original allocation.");
                Assert.AreEqual(80, data->Count);
                for (var i = 0; i < 80; ++i)
                {
                    Assert.IsTrue(data->TryGetValue(i + 1, out byte value));
                    Assert.AreEqual((byte)(i + 20), value);
                }

                AssertRegularStorage(data);
            }, 128);
        }

        [Test]
        public void UntypedMap_SmallValuesAndRawLengths_HaveZeroSlotTails()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicUntypedHashMapHelper<int>.Init(bytes, 1, 1, 0);
                var data = bytes.AsUntypedHelper<int>();
                DynamicUntypedHashMapHelper<int>.AddUnique(bytes, ref data, 101, (byte)7);
                DynamicUntypedHashMapHelper<int>.AddOrSet(bytes, ref data, 102, (ushort)1234);
                DynamicUntypedHashMapHelper<int>.AddUnique(bytes, ref data, 103, 9876543210L);
                byte* source = stackalloc byte[8];
                for (var i = 0; i < 8; ++i)
                {
                    source[i] = (byte)(i + 31);
                }

                for (var length = 0; length <= 7; ++length)
                {
                    DynamicUntypedHashMapHelper<int>.AddUniqueRaw(bytes, ref data, 200 + length, source, length);
                    DynamicUntypedHashMapHelper<int>.AddOrSetRaw(bytes, ref data, 300 + length, source, length);
                }

                Assert.IsTrue(data->TryGetValue(101, out byte small));
                Assert.AreEqual(7, small);
                Assert.IsTrue(data->TryGetValue(102, out ushort medium));
                Assert.AreEqual(1234, medium);
                Assert.IsTrue(data->TryGetValue(103, out long large));
                Assert.AreEqual(9876543210L, large);
                for (var length = 0; length <= 7; ++length)
                {
                    Assert.IsTrue(data->TryGetValueRaw(200 + length, out var raw, out var actual));
                    Assert.AreEqual(length, actual);
                    for (var i = 0; i < length; ++i)
                    {
                        Assert.AreEqual(source[i], raw[i]);
                    }

                    // Self-update must copy before clearing the occupied slot's tail.
                    DynamicUntypedHashMapHelper<int>.AddOrSetRaw(bytes, ref data, 200 + length, raw, length);
                }

                AssertUntypedMapStorage(data);
            });
        }

        [Test]
        public void UntypedMap_RemoveSwapLastAndGrow_PreservesOtherPayloads()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicUntypedHashMapHelper<int>.Init(bytes, 4, 4, 0);
                var data = bytes.AsUntypedHelper<int>();
                DynamicUntypedHashMapHelper<int>.AddUnique(bytes, ref data, 1, 111L);
                DynamicUntypedHashMapHelper<int>.AddUnique(bytes, ref data, 2, 222L);
                DynamicUntypedHashMapHelper<int>.AddUnique(bytes, ref data, 3, (byte)33);
                Assert.AreEqual(1, data->TryRemove(1));
                Assert.IsTrue(data->TryGetValue(2, out long value2));
                Assert.AreEqual(222L, value2);
                Assert.IsTrue(data->TryGetValue(3, out byte value3));
                Assert.AreEqual(33, value3);
                AssertUntypedMapStorage(data);
                DynamicUntypedHashMapHelper<int>.Resize(bytes, ref data, 9);
                DynamicUntypedHashMapHelper<int>.ResizeData(bytes, ref data, 17);
                AssertUntypedMapStorage(data);
                Assert.IsTrue(data->TryGetValue(2, out value2));
                Assert.AreEqual(222L, value2);
                Assert.AreEqual(1, data->TryRemove(2));
                Assert.AreEqual(1, data->TryRemove(3));
                Assert.AreEqual(0, data->Count);
                Assert.AreEqual(0, data->DataAllocatedIndex);
                AssertUntypedMapStorage(data);
            });
        }

        [Test]
        public void UntypedBuffer_GrowthAndRemoval_ClearGapsAndRetiredStorage()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                DynamicUntypedBufferHelper.Init(bytes, 1, 1, 0);
                var data = bytes.AsUntypedBufferHelper();
                DynamicUntypedBufferHelper.Add(bytes, ref data, (byte)7);
                DynamicUntypedBufferHelper.Add(bytes, ref data, 1234);
                DynamicUntypedBufferHelper.Add(bytes, ref data, 9876543210L);
                AssertUntypedBufferStorage(data);
                DynamicUntypedBufferHelper.Resize(bytes, ref data, 9);
                DynamicUntypedBufferHelper.ResizeData(bytes, ref data, 73);
                AssertUntypedBufferStorage(data);
                Assert.AreEqual(7, DynamicUntypedBufferHelper.GetValue<byte>(data, 0));
                Assert.AreEqual(1234, DynamicUntypedBufferHelper.GetValue<int>(data, 1));
                Assert.AreEqual(9876543210L, DynamicUntypedBufferHelper.GetValue<long>(data, 2));

                DynamicUntypedBufferHelper.RemoveAt(ref data, 1);
                Assert.AreEqual(7, DynamicUntypedBufferHelper.GetValue<byte>(data, 0));
                Assert.AreEqual(9876543210L, DynamicUntypedBufferHelper.GetValue<long>(data, 1));
                AssertUntypedBufferStorage(data);
                DynamicUntypedBufferHelper.RemoveAt(ref data, 0);
                Assert.AreEqual(9876543210L, DynamicUntypedBufferHelper.GetValue<long>(data, 0));
                AssertUntypedBufferStorage(data);
                DynamicUntypedBufferHelper.RemoveAt(ref data, 0);
                Assert.AreEqual(0, data->Count);
                AssertUntypedBufferStorage(data);

                DynamicUntypedBufferHelper.Add(bytes, ref data, (ushort)42);
                data->Clear();
                AssertUntypedBufferStorage(data);
                DynamicUntypedBufferHelper.Add(bytes, ref data, 4567L);
                Assert.AreEqual(4567L, DynamicUntypedBufferHelper.GetValue<long>(data, 0));
                AssertUntypedBufferStorage(data);
            });
        }

        [Test]
        public void OneHashColumn_RemoveGrowShrinkAndClear_PreservesLiveValues()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                OneHash.Init(bytes, 4, 0);
                var data = bytes.AsVariableHelper<int, long, byte, MultiHashColumn<byte>>();
                AssertVariableStorage(data);
                OneHash.AddUnique(bytes, ref data, 1, 111L, 5);
                AssertVariableStorage(data);
                OneHash.AddUnique(bytes, ref data, 2, 222L, 6);
                AssertVariableStorage(data);
                OneHash.AddUnique(bytes, ref data, 3, 333L, 5);
                AssertVariableStorage(data);
                Assert.IsTrue(data->Remove(1));
                AssertVariableStorage(data);
                Assert.AreEqual(2, data->Find(3));
                Assert.IsTrue(data->TryGetValue(3, out var value, out var column));
                Assert.AreEqual(333L, value);
                Assert.AreEqual(5, column);
                OneHash.Resize(bytes, ref data, 9);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(3, out value, out column));
                Assert.AreEqual(333L, value);
                Assert.AreEqual(5, column);
                Assert.IsTrue(data->Column.TryGetFirst(5, out var it));
                Assert.AreEqual(data->Find(3), it.EntryIndex);
                Assert.IsFalse(data->Column.TryGetNext(ref it));
                OneHash.Resize(bytes, ref data, 2);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(2, out value, out column));
                Assert.AreEqual(222L, value);
                Assert.AreEqual(6, column);
                data->RemoveAt(data->Find(2));
                AssertVariableStorage(data);
                data->Clear();
                AssertVariableStorage(data);
                AssertZero(data->Values, (long)data->Capacity * sizeof(long));
                AssertZero(data->KeyHash.Keys, (long)data->Capacity * sizeof(int));
                for (var i = 0; i < data->Capacity; ++i)
                {
                    Assert.AreEqual(0, ReadColumn(ref data->Column, i));
                }

                OneHash.AddUnique(bytes, ref data, 4, 444L, 7);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(4, out value, out column));
                Assert.AreEqual(444L, value);
                Assert.AreEqual(7, column);
            });
        }

        [Test]
        public void OneOrderedColumn_RemoveAndGrow_PreservesSortedChain()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                OneOrdered.Init(bytes, 4, 0);
                var data = bytes.AsVariableHelper<int, long, byte, OrderedListColumn<byte>>();
                AssertVariableStorage(data);
                OneOrdered.AddUnique(bytes, ref data, 1, 111L, 30);
                AssertVariableStorage(data);
                OneOrdered.AddUnique(bytes, ref data, 2, 222L, 10);
                AssertVariableStorage(data);
                OneOrdered.AddUnique(bytes, ref data, 3, 333L, 20);
                AssertVariableStorage(data);
                Assert.IsTrue(data->Remove(1));
                AssertVariableStorage(data);
                OneOrdered.Resize(bytes, ref data, 9);
                AssertVariableStorage(data);
                Assert.IsTrue(data->Column.TryGetFirst(out var first, out var it));
                Assert.AreEqual(10, first);
                Assert.IsTrue(data->Column.TryGetNext(out var second, ref it));
                Assert.AreEqual(20, second);
                Assert.IsFalse(data->Column.TryGetNext(out _, ref it));
                Assert.IsTrue(data->TryGetValue(3, out var value, out var column));
                Assert.AreEqual(333L, value);
                Assert.AreEqual(20, column);
                data->RemoveAt(data->Find(2));
                AssertVariableStorage(data);
                Assert.IsTrue(data->Column.TryGetFirst(out first, out it));
                Assert.AreEqual(20, first);
                OneOrdered.Resize(bytes, ref data, 1);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(3, out value, out column));
                Assert.AreEqual(333L, value);
                data->Clear();
                AssertVariableStorage(data);
                Assert.AreEqual(-1, data->Column.GetFirst());
                for (var i = 0; i < data->Capacity; ++i)
                {
                    Assert.AreEqual(0, data->Column.GetValue(i));
                }
            });
        }

        [Test]
        public void TwoColumns_SparseGrowthShrinkAndClear_PreserveBothIndexes()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                TwoColumns.Init(bytes, 4, 0);
                var data = bytes.AsVariableHelper<int, long, byte, MultiHashColumn<byte>, short, OrderedListColumn<short>>();
                AssertVariableStorage(data);
                TwoColumns.AddUnique(bytes, ref data, 1, 111L, 5, 30);
                AssertVariableStorage(data);
                TwoColumns.AddUnique(bytes, ref data, 2, 222L, 6, 10);
                AssertVariableStorage(data);
                TwoColumns.AddUnique(bytes, ref data, 3, 333L, 5, 20);
                AssertVariableStorage(data);
                Assert.IsTrue(data->Remove(1));
                AssertVariableStorage(data);
                TwoColumns.Resize(bytes, ref data, 9);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(3, out var value, out var c1, out var c2));
                Assert.AreEqual(333L, value);
                Assert.AreEqual(5, c1);
                Assert.AreEqual(20, c2);
                Assert.IsTrue(data->Column1.TryGetFirst(5, out var hashIt));
                Assert.AreEqual(data->Find(3), hashIt.EntryIndex);
                Assert.IsFalse(data->Column1.TryGetNext(ref hashIt));
                Assert.IsTrue(data->Column2.TryGetFirst(out var first, out var orderedIt));
                Assert.AreEqual(10, first);
                Assert.IsTrue(data->Column2.TryGetNext(out var second, ref orderedIt));
                Assert.AreEqual(20, second);
                TwoColumns.Resize(bytes, ref data, 2);
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(2, out value, out c1, out c2));
                Assert.AreEqual(222L, value);
                Assert.AreEqual(6, c1);
                Assert.AreEqual(10, c2);
                data->RemoveAt(data->Find(2));
                AssertVariableStorage(data);
                Assert.IsTrue(data->TryGetValue(3, out value, out c1, out c2));
                Assert.AreEqual(333L, value);
                data->Clear();
                AssertVariableStorage(data);
                AssertZero(data->Values, (long)data->Capacity * sizeof(long));
                AssertZero(data->KeyHash.Keys, (long)data->Capacity * sizeof(int));
                for (var i = 0; i < data->Capacity; ++i)
                {
                    Assert.AreEqual(0, ReadColumn(ref data->Column1, i));
                    Assert.AreEqual(0, data->Column2.GetValue(i));
                }
            });
        }

        [Test]
        public void PerfectMap_UnoccupiedKeysAreZeroAndNullValueIsPreserved()
        {
            this.AssertPoisonIndependent(bytes =>
            {
                var keys = new NativeArray<int>(2, Allocator.Temp);
                var values = new NativeArray<long>(2, Allocator.Temp);
                try
                {
                    keys[0] = 0;
                    keys[1] = 2;
                    values[0] = 100L;
                    values[1] = 200L;
                    DynamicPerfectHashMapHelper<int, long>.Init(bytes, keys, values, -7L);
                    var data = bytes.AsHelper<int, long>();
                    Assert.AreEqual(4, data->Size);
                    Assert.AreEqual(0, data->Keys[0]);
                    Assert.AreEqual(2, data->Keys[2]);
                    Assert.AreEqual(100L, data->Values[0]);
                    Assert.AreEqual(200L, data->Values[2]);
                    Assert.AreEqual(0, data->Keys[1]);
                    Assert.AreEqual(0, data->Keys[3]);
                    Assert.AreEqual(-7L, data->Values[1]);
                    Assert.AreEqual(-7L, data->Values[3]);
                    AssertZero((byte*)data + data->KeysOffset + (data->Size * sizeof(int)),
                        data->ValuesOffset - data->KeysOffset - (data->Size * sizeof(int)));
                }
                finally
                {
                    values.Dispose();
                    keys.Dispose();
                }
            });
        }

        private static void AssertVariableStorage(OneHash* data)
        {
            var active = AssertMainVariableStorage((byte*)data, sizeof(OneHash), data->ValuesOffset,
                data->Count, data->Capacity, data->BucketCapacity, data->Values, ref data->KeyHash);
            var start = ColumnStart(ref data->Column);
            AssertZero(data->KeyHash.Buckets + data->BucketCapacity,
                start - (byte*)(data->KeyHash.Buckets + data->BucketCapacity));
            AssertHashColumnStorage(ref data->Column, active);
        }

        private static void AssertVariableStorage(OneOrdered* data)
        {
            var active = AssertMainVariableStorage((byte*)data, sizeof(OneOrdered), data->ValuesOffset,
                data->Count, data->Capacity, data->BucketCapacity, data->Values, ref data->KeyHash);
            var start = ColumnStart(ref data->Column);
            AssertZero(data->KeyHash.Buckets + data->BucketCapacity,
                start - (byte*)(data->KeyHash.Buckets + data->BucketCapacity));
            AssertOrderedColumnStorage(ref data->Column, active);
        }

        private static void AssertVariableStorage(TwoColumns* data)
        {
            var active = AssertMainVariableStorage((byte*)data, sizeof(TwoColumns), data->ValuesOffset,
                data->Count, data->Capacity, data->BucketCapacity, data->Values, ref data->KeyHash);
            var start = ColumnStart(ref data->Column1);
            AssertZero(data->KeyHash.Buckets + data->BucketCapacity,
                start - (byte*)(data->KeyHash.Buckets + data->BucketCapacity));
            var firstEnd = AssertHashColumnStorage(ref data->Column1, active);
            var secondStart = ColumnStart(ref data->Column2);
            AssertZero(firstEnd, secondStart - firstEnd);
            AssertOrderedColumnStorage(ref data->Column2, active);
        }

        private static bool[] AssertMainVariableStorage(byte* data, int headerSize, int valuesOffset,
            int count, int capacity, int bucketCapacity, long* values, ref HashHelper<int> hash)
        {
            var active = new bool[capacity];
            var visited = 0;
            for (var bucket = 0; bucket < bucketCapacity; ++bucket)
            {
                for (var index = hash.Buckets[bucket]; index != -1; index = hash.Next[index])
                {
                    Assert.IsTrue(index >= 0 && index < capacity);
                    Assert.IsFalse(active[index], "Cycle or duplicate entry in primary hash chain.");
                    active[index] = true;
                    ++visited;
                }
            }

            Assert.AreEqual(count, visited);
            for (var i = 0; i < capacity; ++i)
            {
                if (!active[i])
                {
                    AssertZero(hash.Keys + i, sizeof(int));
                    AssertZero(values + i, sizeof(long));
                }
            }

            AssertZero(data + headerSize, valuesOffset - headerSize);
            AssertZero(values + capacity, (byte*)hash.Keys - (byte*)(values + capacity));
            AssertZero(hash.Keys + capacity, (byte*)hash.Next - (byte*)(hash.Keys + capacity));
            AssertZero(hash.Next + capacity, (byte*)hash.Buckets - (byte*)(hash.Next + capacity));
            return active;
        }

        private static byte* ColumnStart<TColumn>(ref TColumn column)
            where TColumn : unmanaged
        {
            // White-box layout check: both shipped columns store keysOffset first.
            var basePtr = (byte*)UnsafeUtility.AddressOf(ref column);
            return basePtr + *(int*)basePtr;
        }

        private static byte* AssertHashColumnStorage(ref MultiHashColumn<byte> column, bool[] active)
        {
            var basePtr = (byte*)UnsafeUtility.AddressOf(ref column);
            var header = (int*)basePtr;
            var keys = basePtr + header[0];
            var next = (int*)(basePtr + header[1]);
            var buckets = (int*)(basePtr + header[2]);
            var capacity = active.Length;
            Assert.AreEqual(capacity, header[3]);
            for (var i = 0; i < capacity; ++i)
            {
                if (!active[i])
                {
                    Assert.AreEqual(0, keys[i]);
                    Assert.AreEqual(-1, next[i]);
                }
            }

            AssertZero(keys + capacity, (byte*)next - (keys + capacity));
            AssertZero(next + capacity, (byte*)buckets - (byte*)(next + capacity));
            return (byte*)(buckets + (capacity * 2));
        }

        private static void AssertOrderedColumnStorage<T>(ref OrderedListColumn<T> column, bool[] active)
            where T : unmanaged, IEquatable<T>, IComparable<T>
        {
            var basePtr = (byte*)UnsafeUtility.AddressOf(ref column);
            var header = (int*)basePtr;
            var keys = (T*)(basePtr + header[0]);
            var next = (int*)(basePtr + header[1]);
            var prev = (int*)(basePtr + header[2]);
            var capacity = active.Length;
            Assert.AreEqual(capacity, header[4]);
            for (var i = 0; i < capacity; ++i)
            {
                if (!active[i])
                {
                    AssertZero(keys + i, sizeof(T));
                    Assert.AreEqual(-1, next[i]);
                    Assert.AreEqual(-1, prev[i]);
                }
            }

            AssertZero(keys + capacity, (byte*)next - (byte*)(keys + capacity));
            AssertZero(next + capacity, (byte*)prev - (byte*)(next + capacity));
        }

        private static byte ReadColumn(ref MultiHashColumn<byte> column, int index)
        {
            return ReadColumnValue<MultiHashColumn<byte>, byte>(ref column, index);
        }

        private static T ReadColumnValue<TColumn, T>(ref TColumn column, int index)
            where TColumn : unmanaged, IColumn<T>
            where T : unmanaged, IEquatable<T>
        {
            return column.GetValue(index);
        }

        private void AssertPoisonIndependent(Action<DynamicBuffer<byte>> scenario, int reserve = 65536)
        {
            // Finish each scenario before creating the next entity: structural changes can
            // invalidate previously acquired DynamicBuffer safety handles.
            var first = this.Capture(0xa5, scenario, reserve);
            var second = this.Capture(0x5a, scenario, reserve);
            CollectionAssert.AreEqual(first, second, "Logical byte buffers depend on the original allocation contents.");
        }

        private byte[] Capture(byte poison, Action<DynamicBuffer<byte>> scenario, int reserve)
        {
            var entity = this.Manager.CreateEntity(typeof(PackedMemoryTestByte));
            var bytes = this.Manager.GetBuffer<PackedMemoryTestByte>(entity).Reinterpret<byte>();
            // Keep enough capacity to exercise overlapping old/new layouts without a
            // physical reallocation hiding poison bytes behind a fresh zeroed allocation.
            bytes.ResizeUninitialized(reserve);
            UnsafeUtility.MemSet(bytes.GetUnsafePtr(), poison, bytes.Length);
            bytes.ResizeUninitialized(0);
            scenario(bytes);
            return bytes.AsNativeArray().ToArray();
        }

        private static void AddRegular(DynamicBuffer<byte> bytes, ref DynamicHashMapHelper<int>* data, int key, byte value)
        {
            var index = DynamicHashMapHelper<int>.AddUnique(bytes, ref data, key);
            data->Values[index] = value;
        }

        private static void AssertRegularStorage(DynamicHashMapHelper<int>* data)
        {
            var active = new bool[data->Capacity];
            var visited = 0;
            for (var bucket = 0; bucket < data->BucketCapacity; ++bucket)
            {
                for (var index = data->Buckets[bucket]; index != -1; index = data->Next[index])
                {
                    Assert.IsTrue(index >= 0 && index < data->Capacity);
                    Assert.IsFalse(active[index], "Cycle or duplicate entry in hash chain.");
                    active[index] = true;
                    ++visited;
                }
            }

            Assert.AreEqual(data->Count, visited);
            for (var i = 0; i < data->Capacity; ++i)
            {
                if (!active[i])
                {
                    AssertZero(data->Keys + i, sizeof(int));
                    AssertZero(data->Values + ((long)i * data->SizeOfTValue), data->SizeOfTValue);
                }
            }

            AssertZero((byte*)data + sizeof(DynamicHashMapHelper<int>), data->ValuesOffset - sizeof(DynamicHashMapHelper<int>));
            var end = (long)data->ValuesOffset + ((long)data->Capacity * data->SizeOfTValue);
            AssertZero((byte*)data + end, data->KeysOffset - end);
            end = data->KeysOffset + ((long)data->Capacity * sizeof(int));
            AssertZero((byte*)data + end, data->NextOffset - end);
            end = data->NextOffset + ((long)data->Capacity * sizeof(int));
            AssertZero((byte*)data + end, data->BucketsOffset - end);
        }

        private static void AssertUntypedMapStorage(DynamicUntypedHashMapHelper<int>* data)
        {
            var used = new bool[data->DataCapacity * sizeof(int)];
            for (var i = 0; i < data->Count; ++i)
            {
                var size = data->Sizes[i];
                if (size <= sizeof(int))
                {
                    AssertZero(data->Values + (i * sizeof(int)) + size, sizeof(int) - size);
                }
                else
                {
                    var offset = ((int*)data->Values)[i] * sizeof(int);
                    Assert.IsTrue(offset >= 0 && offset + size <= used.Length);
                    for (var j = 0; j < size; ++j)
                    {
                        Assert.IsFalse(used[offset + j], "Overlapping payloads.");
                        used[offset + j] = true;
                    }
                }
            }

            AssertZero(data->Keys + data->Count, (long)(data->Capacity - data->Count) * sizeof(int));
            AssertZero(data->Values + (data->Count * sizeof(int)), (long)(data->Capacity - data->Count) * sizeof(int));
            AssertZero(data->Sizes + data->Count, (long)(data->Capacity - data->Count) * sizeof(ushort));
            var arena = (byte*)data->Data;
            for (var i = 0; i < used.Length; ++i)
            {
                if (!used[i])
                {
                    Assert.AreEqual(0, arena[i], $"Unused data byte {i}");
                }
            }

            var end = data->SizesOffset + ((long)data->Capacity * sizeof(ushort));
            AssertZero((byte*)data + end, data->DataOffset - end);
        }

        private static void AssertUntypedBufferStorage(DynamicUntypedBufferHelper* data)
        {
            var end = 0;
            for (var i = 0; i < data->Count; ++i)
            {
                Assert.IsTrue(data->Offsets[i] >= end);
                AssertZero(data->Data + end, data->Offsets[i] - end);
                end = data->Offsets[i] + data->Sizes[i];
            }

            Assert.AreEqual(end, data->DataAllocatedIndex);
            AssertZero(data->Data + end, data->DataCapacity - end);
            var unused = data->Capacity - data->Count;
            AssertZero(data->Offsets + data->Count, (long)unused * sizeof(int));
            AssertZero(data->Sizes + data->Count, (long)unused * sizeof(int));
            AssertZero(data->Types + data->Count, (long)unused * sizeof(int));
            AssertZero(data->Alignments + data->Count, unused);
            var metadataEnd = data->AlignmentsOffset + data->Capacity;
            AssertZero((byte*)data + metadataEnd, data->DataOffset - metadataEnd);
        }

        private static void AssertZero(void* pointer, long length)
        {
            Assert.IsTrue(length >= 0, "Invalid clear-range bounds.");
            var bytes = (byte*)pointer;
            for (long i = 0; i < length; ++i)
            {
                Assert.AreEqual(0, bytes[i], $"Expected zero at relative byte {i} of {length}.");
            }
        }
    }

    [InternalBufferCapacity(0)]
    public struct PackedMemoryTestByte : IBufferElementData
    {
        public byte Value;
    }
}
