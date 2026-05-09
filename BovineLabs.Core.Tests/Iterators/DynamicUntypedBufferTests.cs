// <copyright file="DynamicUntypedBufferTests.cs" company="BovineLabs">
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

    public class DynamicUntypedBufferTests : ECSTestsFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            TypeManagerEx.Initialize();
        }

        [Test]
        public void InitializeAndReadMixedTypes()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(1);
            untyped.Add((short)2);
            untyped.Add(new float3(3, 4, 5));
            untyped.Add(new Large { TestValue0 = 10, TestValue1 = 20 });

            Assert.AreEqual(4, untyped.Length);
            Assert.AreEqual(1, untyped.ElementAtRO<int>(0));
            Assert.AreEqual((short)2, untyped.ElementAtRO<short>(1));
            Assert.AreEqual(new float3(3, 4, 5), untyped.ElementAtRO<float3>(2));

            ref readonly var large = ref untyped.ElementAtRO<Large>(3);
            Assert.AreEqual(10UL, large.TestValue0);
            Assert.AreEqual(20UL, large.TestValue1);
        }

        [Test]
        public void Set_OverwritesExisting()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(new Large { TestValue0 = 1, TestValue1 = 2 });
            untyped.Set(0, new Large { TestValue0 = 3, TestValue1 = 4 });

            ref readonly var large = ref untyped.ElementAtRO<Large>(0);
            Assert.AreEqual(3UL, large.TestValue0);
            Assert.AreEqual(4UL, large.TestValue1);
        }

        [Test]
        public void RemoveAt_CompactsAndPreservesOrder()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(7);
            untyped.Add(new float3(1, 2, 3));
            untyped.Add(new Large { TestValue0 = 11, TestValue1 = 22 });
            untyped.Add((short)9);

            untyped.RemoveAt(1);

            Assert.AreEqual(3, untyped.Length);
            Assert.AreEqual(7, untyped.ElementAtRO<int>(0));

            ref readonly var large = ref untyped.ElementAtRO<Large>(1);
            Assert.AreEqual(11UL, large.TestValue0);
            Assert.AreEqual(22UL, large.TestValue1);

            Assert.AreEqual((short)9, untyped.ElementAtRO<short>(2));
        }

        [Test]
        public unsafe void LargeValuePointer_IsAligned_WhenMixedTypes()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(new float3(1, 2, 3));
            untyped.Add(new Large { TestValue0 = 10, TestValue1 = 20 });

            var helper = untyped.Helper;
            var offset = helper->Offsets[1];
            var ptr = helper->Data + offset;

            var align = UnsafeUtility.AlignOf<Large>();
            Assert.AreEqual(0u, ((ulong)ptr) % (ulong)align);
        }

        [Test]
        public unsafe void RemoveAt_KeepsLargeAlignment()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(new float3(1, 2, 3));
            untyped.Add(new Large { TestValue0 = 10, TestValue1 = 20 });

            untyped.RemoveAt(0);

            var helper = untyped.Helper;
            var offset = helper->Offsets[0];
            var ptr = helper->Data + offset;

            var align = UnsafeUtility.AlignOf<Large>();
            Assert.AreEqual(0u, ((ulong)ptr) % (ulong)align);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
        [Test]
        public void ElementAt_WhenTypeDoesNotMatch_Throws()
        {
            var entity = this.Manager.CreateEntity(typeof(DynamicUntypedBufferTestsBuffer));
            var buffer = this.Manager.GetBuffer<DynamicUntypedBufferTestsBuffer>(entity);

            var untyped = buffer.InitializeUntypedBuffer().AsUntypedBuffer();

            untyped.Add(1);

            Assert.Throws<InvalidOperationException>(() =>
            {
                _ = untyped.ElementAtRO<short>(0);
            });
        }
#endif

        public struct Large
        {
            public ulong TestValue0;
            public ulong TestValue1;
        }
    }
}
