// <copyright file="DynamicGeneratorTestBuffers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapTestsLongBuffer : IDynamicHashMap<int, long>
    {
        byte IDynamicHashMap<int, long>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapPerformanceTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicUntypedBufferTestsBuffer : IDynamicUntypedBuffer
    {
        byte IDynamicUntypedBuffer.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicVariableMapTestsBuffer : IDynamicVariableMap<int, float, short, MultiHashColumn<short>>
    {
        byte IDynamicVariableMap<int, float, short, MultiHashColumn<short>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicVariableMapTestsLongKeyShortValueBuffer : IDynamicVariableMap<long, short, short, MultiHashColumn<short>>
    {
        byte IDynamicVariableMap<long, short, short, MultiHashColumn<short>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicPerfectHashMapTestsBuffer : IDynamicPerfectHashMap<int, short>
    {
        byte IDynamicPerfectHashMap<int, short>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicPerfectHashMapTestsByteLongBuffer : IDynamicPerfectHashMap<byte, long>
    {
        byte IDynamicPerfectHashMap<byte, long>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicVariableMap2TestsBuffer : IDynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>
    {
        byte IDynamicVariableMap<int, float, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicVariableMap2TestsLongKeyShortValueBuffer : IDynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>
    {
        byte IDynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicUntypedHashMapStressTestsBuffer : IDynamicUntypedHashMap<int>
    {
        byte IDynamicUntypedHashMap<int>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicUntypedHashMapTestsBuffer : IDynamicUntypedHashMap<int>
    {
        byte IDynamicUntypedHashMap<int>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicUntypedHashMapTestsLongKeyBuffer : IDynamicUntypedHashMap<long>
    {
        byte IDynamicUntypedHashMap<long>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapTestsBuffer : IDynamicMultiHashMap<int, byte>
    {
        byte IDynamicMultiHashMap<int, byte>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapTestsLongBuffer : IDynamicMultiHashMap<int, long>
    {
        byte IDynamicMultiHashMap<int, long>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicHashSetTestsBuffer : IDynamicHashSet<int>
    {
        byte IDynamicHashSet<int>.Value { get; }
    }
}
