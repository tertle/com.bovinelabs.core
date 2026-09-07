// <copyright file="CollectionInitializationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using BovineLabs.Core.Extensions;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe class CollectionInitializationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ResizeInitialized_UsesElementSizeAndDoesNotTouchGuard(bool useFillByte)
        {
            CheckResize<byte>(useFillByte);
            CheckResize<ushort>(useFillByte);
            CheckResize<int>(useFillByte);
            CheckResize<long>(useFillByte);
            CheckResize<WideValue>(useFillByte);
        }

        [Test]
        public void CopyNativeHashSetToList_UsesLiveCountAfterRemoval()
        {
            var set = new NativeParallelHashSet<int>(16, Allocator.Temp);
            var list = new NativeList<int>(16, Allocator.Temp);
            try
            {
                set.Add(11);
                set.Add(22);
                set.Add(33);
                Assert.IsTrue(set.Remove(22));
                list.ResizeUninitialized(16);
                UnsafeUtility.MemSet(list.GetUnsafePtr(), 0xa5, 16 * sizeof(int));

                set.CopyToNativeList(list);

                Assert.AreEqual(set.Count(), list.Length);
                CollectionAssert.AreEquivalent(new[] { 11, 33 }, list.AsArray().ToArray());
                set.Remove(11);
                set.Remove(33);
                set.CopyToNativeList(list);
                Assert.AreEqual(0, list.Length);
            }
            finally
            {
                list.Dispose();
                set.Dispose();
            }
        }

        [Test]
        public void CopyUnsafeHashSetToList_UsesLiveCountAfterRemoval()
        {
            var set = new UnsafeParallelHashSet<int>(16, Allocator.Temp);
            var list = new NativeList<int>(16, Allocator.Temp);
            try
            {
                set.Add(11);
                set.Add(22);
                set.Add(33);
                Assert.IsTrue(set.Remove(22));
                list.ResizeUninitialized(16);
                UnsafeUtility.MemSet(list.GetUnsafePtr(), 0x5a, 16 * sizeof(int));

                set.CopyToNativeList(list);

                Assert.AreEqual(set.Count(), list.Length);
                CollectionAssert.AreEquivalent(new[] { 11, 33 }, list.AsArray().ToArray());
                set.Remove(11);
                set.Remove(33);
                set.CopyToNativeList(list);
                Assert.AreEqual(0, list.Length);
            }
            finally
            {
                list.Dispose();
                set.Dispose();
            }
        }

        private static void CheckResize<T>(bool useFillByte)
            where T : unmanaged
        {
            // Spare allocation is a guard, not part of the list being initialized.
            const int guardElements = 128;
            const int requestedElements = 8;
            const byte poison = 0xa5;
            const byte fill = 0x3c;
            var list = new NativeList<T>(guardElements, Allocator.Temp);
            try
            {
                list.ResizeUninitialized(guardElements);
                UnsafeUtility.MemSet(list.GetUnsafePtr(), poison, (long)sizeof(T) * guardElements);
                list.ResizeUninitialized(0);

                if (useFillByte)
                {
                    list.ResizeInitialized(requestedElements, fill);
                }
                else
                {
                    list.ResizeInitialized(requestedElements);
                }

                Assert.AreEqual(requestedElements, list.Length);
                var bytes = (byte*)list.GetUnsafeReadOnlyPtr();
                var writtenBytes = sizeof(T) * requestedElements;
                for (var i = 0; i < sizeof(T) * guardElements; ++i)
                {
                    var expected = i < writtenBytes ? (useFillByte ? fill : (byte)0) : poison;
                    Assert.AreEqual(expected, bytes[i], $"{typeof(T).Name}: byte {i}");
                }

                // A zero-length call must not touch any backing memory.
                UnsafeUtility.MemSet(list.GetUnsafePtr(), poison, (long)sizeof(T) * guardElements);
                if (useFillByte)
                {
                    list.ResizeInitialized(0, fill);
                }
                else
                {
                    list.ResizeInitialized(0);
                }

                bytes = (byte*)list.GetUnsafeReadOnlyPtr();
                for (var i = 0; i < sizeof(T) * guardElements; ++i)
                {
                    Assert.AreEqual(poison, bytes[i]);
                }
            }
            finally
            {
                list.Dispose();
            }
        }

        private struct WideValue
        {
            public long A;
            public long B;
            public long C;
        }
    }
}
