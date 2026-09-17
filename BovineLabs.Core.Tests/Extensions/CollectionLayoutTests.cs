namespace BovineLabs.Core.Tests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;

    public unsafe class CollectionLayoutTests
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static IEnumerable<TestCaseData> Layouts()
        {
            var assembly = typeof(NativeHashMap<int, int>).Assembly;
            var helper = assembly.GetType("Unity.Collections.LowLevel.Unsafe.HashMapHelper`1", true).MakeGenericType(typeof(int));
            yield return new TestCaseData(helper, typeof(HashMapHelper<int>)).SetName("Layout_HashMapHelper");
            yield return new TestCaseData(assembly.GetType("Unity.Collections.LowLevel.Unsafe.UnsafeParallelHashMapData", true),
                typeof(UnsafeParallelHashMapData)).SetName("Layout_ParallelHashMapData");
        }

        [TestCaseSource(nameof(Layouts))]
        public void LayoutMatchesUnity(Type actual, Type wrapper)
        {
            var size = typeof(UnsafeUtility).GetMethod(nameof(UnsafeUtility.SizeOf), Type.EmptyTypes);
            var align = typeof(UnsafeUtility).GetMethod(nameof(UnsafeUtility.AlignOf), Type.EmptyTypes);
            Assert.That(size.MakeGenericMethod(wrapper).Invoke(null, null), Is.EqualTo(size.MakeGenericMethod(actual).Invoke(null, null)), "Size");
            Assert.That(align.MakeGenericMethod(wrapper).Invoke(null, null), Is.EqualTo(align.MakeGenericMethod(actual).Invoke(null, null)), "Alignment");
            var actualFields = actual.GetFields(Fields);
            var wrapperFields = wrapper.GetFields(Fields);
            Assert.That(wrapperFields.Length, Is.EqualTo(actualFields.Length), "Every instance field must be represented");
            foreach (var field in actualFields)
            {
                var mirror = wrapper.GetField(field.Name, Fields);
                Assert.That(mirror, Is.Not.Null, field.Name);
                Assert.That(UnsafeUtility.GetFieldOffset(mirror), Is.EqualTo(UnsafeUtility.GetFieldOffset(field)), field.Name);
            }
        }

        [Test]
        public void CoreTypeForwardersResolveToTheBridge()
        {
            var core = typeof(BovineLabs.Core.Jobs.JobHashMapDefer).Assembly;
            foreach (var type in new[]
            {
                typeof(HashMapHelper<>), typeof(UnsafeParallelHashMapData), typeof(Bitwise), typeof(MemoryUtil), typeof(CollectionInternal),
                typeof(NativeHashMapFactory<,>), typeof(NativeHashSetFactory<>),
            })
            {
                Assert.That(type.Assembly.GetName().Name, Is.EqualTo("Unity.Collections"));
                Assert.That(core.GetType(type.FullName, true), Is.EqualTo(type));
            }
        }

        [Test]
        public void SafetyAccessorReturnsTheOriginalHandleByReference()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var list = new NativeList<int>(Allocator.Temp);
            var original = list.GetSafety();
            var replacement = AtomicSafetyHandle.Create();
            try
            {
                list.GetSafety() = replacement;
                Assert.That(NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list), Is.EqualTo(replacement));
            }
            finally
            {
                list.GetSafety() = original;
                AtomicSafetyHandle.Release(replacement);
                list.Dispose();
            }
#endif
        }

        [Test]
        public void MemoryLabelCreationRunsInBurst()
        {
            using var result = new NativeReference<int>(Allocator.TempJob);
            new CreateLabel
            {
                Category = "Core tests",
                Name = "Burst collections bridge",
                Result = result,
            }.Schedule().Complete();
            Assert.That(result.Value, Is.EqualTo(1));
        }

        [Test]
        public void UnsafeMapRemovalUpdatesOriginalCountAndReusesEntry()
        {
            using var map = new UnsafeHashMap<int, long>(4, Allocator.Temp);
            map.Add(1, 11);
            map.Add(2, 22);
            var mutable = map;
            Assert.That(mutable.Remove(1, out var removed), Is.True);
            Assert.That(removed, Is.EqualTo(11));
            Assert.That(mutable.Count, Is.EqualTo(1));
            mutable.GetOrAddRefUnsafe(3, 33) = 44;
            Assert.That(mutable.Count, Is.EqualTo(2));
            Assert.That(mutable[3], Is.EqualTo(44));
            Assert.That(mutable.ContainsKey(1), Is.False);
        }

        [Test]
        public void NativeMapFactorySupportsBurstGrowthAndEnumeration()
        {
            using var map = NativeHashMapFactory<int, long>.Create(0, 1, Allocator.TempJob);
            new PopulateMap
            {
                Map = map,
            }.Schedule().Complete();
            Assert.That(map.Count, Is.EqualTo(128));
            long sum = 0;
            foreach (var pair in map)
            {
                Assert.That(pair.Value, Is.EqualTo(pair.Key * 3L));
                sum += pair.Value;
            }

            Assert.That(sum, Is.EqualTo(24384));
        }

        [Test]
        public void CustomAllocatorSupportsWrapperAllocationGrowthAndFree()
        {
            var allocator = new AllocatorHelper<BovineLabs.Core.Memory.MemoryLabelAllocator>(Allocator.Persistent);
            allocator.Allocator.Initialize("Core tests", "Collections layout");
            try
            {
                using var map = NativeHashMapFactory<int, long>.Create(0, 1, allocator.Allocator.Handle);
                new PopulateMap
                {
                    Map = map,
                }.Schedule().Complete();
                Assert.That(map.Count, Is.EqualTo(128));
                Assert.That(map[127], Is.EqualTo(381));
            }
            finally
            {
                allocator.Allocator.Dispose();
                allocator.Dispose();
            }
        }

        [Test]
        public void MultiMapPreservesDuplicatesThroughGrowthEnumerationAndScheduledDisposal()
        {
            var map = new NativeMultiHashMap<int, long>(0, Allocator.TempJob);
            for (var i = 0; i < 512; i++)
            {
                map.Add(i % 3, i);
            }

            Assert.That(map.Count, Is.EqualTo(512));
            long sum = 0;
            foreach (var pair in map)
            {
                sum += pair.Value;
            }

            Assert.That(sum, Is.EqualTo(130816));
            map.Dispose(default(JobHandle)).Complete();
            Assert.That(map.IsCreated, Is.False);

            var unsafeMap = new UnsafeMultiHashMap<int, long>(0, Allocator.TempJob, 1);
            unsafeMap.Add(1, 10);
            unsafeMap.Add(1, 20);
            Assert.That(unsafeMap.Count, Is.EqualTo(2));
            sum = 0;
            foreach (var pair in unsafeMap)
            {
                sum += pair.Value;
            }

            Assert.That(sum, Is.EqualTo(30));
            unsafeMap.Dispose(default(JobHandle)).Complete();
            Assert.That(unsafeMap.IsCreated, Is.False);
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct CreateLabel : IJob
        {
            public FixedString32Bytes Category;
            public FixedString64Bytes Name;
            public NativeReference<int> Result;

            public void Execute()
            {
                this.Result.Value = MemoryUtil.CreateLabel(this.Category, this.Name).IsCreated ? 1 : 0;
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct PopulateMap : IJob
        {
            public NativeHashMap<int, long> Map;

            public void Execute()
            {
                for (var i = 0; i < 128; i++)
                {
                    this.Map.GetOrAddRefUnsafe(i) = i * 3L;
                }
            }
        }
    }
}
