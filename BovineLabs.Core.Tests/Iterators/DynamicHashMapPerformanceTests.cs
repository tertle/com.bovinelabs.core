// <copyright file="DynamicHashMapPerformanceTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.PerformanceTesting;
    using Random = Unity.Mathematics.Random;

    public class DynamicHashMapPerformanceTests : ECSTestsFixture
    {
        private const uint RandomSeed1 = 2210602657;
        private const uint RandomSeed2 = 2210602658;
        private const int MinGrowth = 64;

        [Test]
        [Performance]
        public void Insert_Sequential()
        {
            const int insertions = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    for (var i = 0; i < insertions; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void Insert_Random()
        {
            const int insertions = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();
                    var random = new Random(RandomSeed1);

                    for (var i = 0; i < insertions; i++)
                    {
                        var key = random.NextInt();
                        hashMap.TryAdd(key, (byte)(i % 255));
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void IndexerWrite_ExistingKeys()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Pre-populate with keys
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Test indexer writes on existing keys (this tests the redundant lookup issue)
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap[i] = (byte)((i + 1) % 255);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void IndexerWrite_NewKeys()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Test indexer writes on new keys
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap[i] = (byte)(i % 255);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void IndexerWrite_Mixed()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Pre-populate half the keys
                    for (var i = 0; i < operations / 2; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Test mixed indexer writes (existing + new keys)
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap[i] = (byte)((i + 1) % 255);
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void TryGetValue_Sequential()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Pre-populate
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Test lookups
                    var sum = 0;
                    for (var i = 0; i < operations; i++)
                    {
                        if (hashMap.TryGetValue(i, out var value))
                        {
                            sum += value;
                        }
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void TryGetValue_Random()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();
                    var random = new Random(RandomSeed1);
                    var keys = new NativeArray<int>(operations, Allocator.Temp);

                    // Pre-populate with random keys
                    for (var i = 0; i < operations; i++)
                    {
                        var key = random.NextInt();
                        keys[i] = key;
                        hashMap.TryAdd(key, (byte)(i % 255));
                    }

                    // Test random lookups
                    random = new Random(RandomSeed2);
                    var sum = 0;
                    for (var i = 0; i < operations; i++)
                    {
                        var key = keys[random.NextInt(0, operations - 1)];
                        if (hashMap.TryGetValue(key, out var value))
                        {
                            sum += value;
                        }
                    }

                    keys.Dispose();
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void Enumerate_Small()
        {
            const int size = 1000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Pre-populate
                    for (var i = 0; i < size; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Test enumeration (this tests cache locality)
                    var sum = 0;
                    foreach (var kvp in hashMap)
                    {
                        sum += kvp.Value;
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void Enumerate_Large()
        {
            const int size = 50000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Pre-populate
                    for (var i = 0; i < size; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Test enumeration (this tests cache locality)
                    var sum = 0;
                    foreach (var kvp in hashMap)
                    {
                        sum += kvp.Value;
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void Resize_Growth()
        {
            const int initialSize = 100;
            const int finalSize = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // Start with small capacity
                    for (var i = 0; i < initialSize; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }

                    // Force multiple resize operations
                    for (var i = initialSize; i < finalSize; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void AddBatchUnsafe_Performance()
        {
            const int count = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();
                    var keys = new NativeArray<int>(count, Allocator.Temp);
                    var values = new NativeArray<byte>(count, Allocator.Temp);

                    for (var i = 0; i < count; i++)
                    {
                        keys[i] = i;
                        values[i] = (byte)(i % 255);
                    }

                    hashMap.AddBatchUnsafe(keys, values);

                    keys.Dispose();
                    values.Dispose();
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        [Test]
        [Performance]
        public void AddBatchUnsafe_vs_Individual()
        {
            const int count = 5000;

            // Test individual adds
            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    for (var i = 0; i < count; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(5)
                .Run();

            // Test batch add
            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();
                    var keys = new NativeArray<int>(count, Allocator.Temp);
                    var values = new NativeArray<byte>(count, Allocator.Temp);

                    for (var i = 0; i < count; i++)
                    {
                        keys[i] = i;
                        values[i] = (byte)(i % 255);
                    }

                    hashMap.AddBatchUnsafe(keys, values);

                    keys.Dispose();
                    values.Dispose();
                })
                .WarmupCount(3)
                .MeasurementCount(5)
                .Run();
        }

        [Test]
        [Performance]
        public void LoadFactor_Performance()
        {
            const int capacity = 10000;

            // Test different load factors: 25%, 50%, 75%, 95%
            var loadFactors = new[] { 0.25f, 0.5f, 0.75f, 0.95f };

            foreach (var loadFactor in loadFactors)
            {
                var itemCount = (int)(capacity * loadFactor);

                Measure
                    .Method(() =>
                    {
                        var hashMap = this.CreateHashMap();
                        hashMap.Capacity = capacity; // Pre-allocate to avoid resize during test

                        // Add items
                        for (var i = 0; i < itemCount; i++)
                        {
                            hashMap.TryAdd(i, (byte)(i % 255));
                        }

                        // Test lookup performance at this load factor
                        var random = new Random(RandomSeed1);
                        var sum = 0;
                        for (var i = 0; i < 1000; i++)
                        {
                            var key = random.NextInt(0, itemCount - 1);
                            if (hashMap.TryGetValue(key, out var value))
                            {
                                sum += value;
                            }
                        }
                    })
                    .WarmupCount(3)
                    .MeasurementCount(5)
                    .Run();
            }
        }

        [Test]
        [Performance]
        public void Memory_Allocation_Tracking()
        {
            const int operations = 10000;

            Measure
                .Method(() =>
                {
                    var hashMap = this.CreateHashMap();

                    // This test helps track memory allocation patterns
                    for (var i = 0; i < operations; i++)
                    {
                        hashMap.TryAdd(i, (byte)(i % 255));

                        // Periodic capacity checks to trigger potential resize operations
                        if (i % 1000 == 0)
                        {
                            _ = hashMap.Capacity;
                        }
                    }
                })
                .WarmupCount(3)
                .MeasurementCount(10)
                .Run();
        }

        private DynamicHashMap<int, byte> CreateHashMap()
        {
            var entity = this.Manager.CreateEntity(typeof(TestHashMap));
            return this.Manager.GetBuffer<TestHashMap>(entity).InitializeHashMap<TestHashMap, int, byte>(0, MinGrowth).AsHashMap<TestHashMap, int, byte>();
        }

        private struct TestHashMap : IDynamicHashMap<int, byte>
        {
            byte IDynamicHashMap<int, byte>.Value { get; }
        }
    }
}
