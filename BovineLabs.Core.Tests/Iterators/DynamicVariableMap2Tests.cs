namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using BovineLabs.Testing;
    using JetBrains.Annotations;
    using NUnit.Framework;
    using Unity.Collections.LowLevel.Unsafe;

    public class DynamicVariableMap2Tests : ECSTestsFixture
    {
        [Test]
        public unsafe void KeysAndColumnsPointers_AreAligned_SmallCapacity()
        {
            var map = CreateSmallCapacityMap();

            var helper = map.Helper;
            Assert.IsNotNull((IntPtr)helper, "Helper pointer should not be null");

            Assert.AreEqual(0ul, (ulong)helper->Values % (ulong)UnsafeUtility.AlignOf<short>(), "Values pointer should be aligned");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Keys % (ulong)UnsafeUtility.AlignOf<long>(), "Keys pointer should be aligned to TKey");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Next % (ulong)UnsafeUtility.AlignOf<int>(), "Next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)helper->KeyHash.Buckets % (ulong)UnsafeUtility.AlignOf<int>(), "Buckets pointer should be aligned to int");

            // Column1: MultiHashColumn<short>
            ref var column1 = ref map.Column1;
            ref var layout1 = ref UnsafeUtility.As<MultiHashColumn<short>, MultiHashColumnLayout<short>>(ref column1);
            var column1Ptr = (byte*)UnsafeUtility.AddressOf(ref column1);
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.KeysOffset) % (ulong)UnsafeUtility.AlignOf<short>(), "Column1 keys pointer should be aligned to T1");
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.NextOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column1 next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)(column1Ptr + layout1.BucketsOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column1 buckets pointer should be aligned to int");

            // Column2: MultiHashColumn<byte> (worst case for int alignment when capacity is small)
            ref var column2 = ref map.Column2;
            ref var layout2 = ref UnsafeUtility.As<MultiHashColumn<byte>, MultiHashColumnLayout<byte>>(ref column2);
            var column2Ptr = (byte*)UnsafeUtility.AddressOf(ref column2);
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.KeysOffset) % (ulong)UnsafeUtility.AlignOf<byte>(), "Column2 keys pointer should be aligned to T2");
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.NextOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column2 next pointer should be aligned to int");
            Assert.AreEqual(0ul, (ulong)(column2Ptr + layout2.BucketsOffset) % (ulong)UnsafeUtility.AlignOf<int>(), "Column2 buckets pointer should be aligned to int");
        }


        private DynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>> CreateMap(int growth = 64)
        {
            var entity = Manager.CreateEntity(typeof(DynamicVariableMap2TestsBuffer));
            return Manager
                .GetBuffer<DynamicVariableMap2TestsBuffer>(entity)
                .InitializeVariableMap<DynamicVariableMap2TestsBuffer, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, growth)
                .AsVariableMap<DynamicVariableMap2TestsBuffer, int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
        }

        private DynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>> CreateSmallCapacityMap()
        {
            var entity = Manager.CreateEntity(typeof(DynamicVariableMap2TestsLongKeyShortValueBuffer));
            return Manager
                .GetBuffer<DynamicVariableMap2TestsLongKeyShortValueBuffer>(entity)
                .InitializeVariableMap<DynamicVariableMap2TestsLongKeyShortValueBuffer, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>(0, 1)
                .AsVariableMap<DynamicVariableMap2TestsLongKeyShortValueBuffer, long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>();
        }

        private struct MultiHashColumnLayout<T>
            where T : unmanaged, IEquatable<T>
        {
            public int KeysOffset;
            public int NextOffset;
            public int BucketsOffset;
            public int Capacity;
        }
    }
}
