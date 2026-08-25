// <copyright file="DynamicGeneratorTestBuffers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Entities;
    using Unity.NetCode;

    [GhostDynamicHashMap(SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap(
        CodecMode = GhostDynamicHashMapCodecMode.RawStable,
        SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapRawStableModeTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapRootOnlyTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap(
        PrefabType = GhostPrefabType.PredictedClient,
        SendTypeOptimization = GhostSendType.OnlyPredictedClients,
        OwnerSendType = SendToOwnerType.SendToOwner,
        SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapPredictedOwnerTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap(
        PrefabType = GhostPrefabType.InterpolatedClient,
        SendTypeOptimization = GhostSendType.OnlyInterpolatedClients,
        OwnerSendType = SendToOwnerType.SendToNonOwner,
        SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapInterpolatedNonOwnerTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapTestsLongBuffer : IDynamicHashMap<int, long>
    {
        byte IDynamicHashMap<int, long>.Value { get; }
    }

    [GhostDynamicHashMap]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapGeneratedStructTestsBuffer : IDynamicHashMap<GeneratedPaddedKey, GeneratedMixedValue>
    {
        byte IDynamicHashMap<GeneratedPaddedKey, GeneratedMixedValue>.Value { get; }
    }

    [GhostDynamicHashMap]
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapGeneratedPaddingTestsBuffer : IDynamicHashMap<int, GeneratedPaddedValue>
    {
        byte IDynamicHashMap<int, GeneratedPaddedValue>.Value { get; }
    }

    internal struct GeneratedPaddedKey : IEquatable<GeneratedPaddedKey>
    {
        public byte A;
        public int B;
        public ushort @event;

        public bool Equals(GeneratedPaddedKey other)
        {
            return this.A == other.A && this.B == other.B && this.@event == other.@event;
        }

        public override int GetHashCode()
        {
            return (((this.B * 397) ^ this.A) * 397) ^ this.@event;
        }
    }

    internal struct GeneratedPaddedValue
    {
        public byte A;
        public int B;
    }

    internal struct GeneratedStableKey : IEquatable<GeneratedStableKey>
    {
        public int Id;
        public int Version;

        public bool Equals(GeneratedStableKey other)
        {
            return this.Id == other.Id && this.Version == other.Version;
        }

        public override int GetHashCode()
        {
            return (this.Id * 397) ^ this.Version;
        }
    }

    internal struct GeneratedNestedValue
    {
        public ushort Count;
        public bool Flag;
    }

    internal enum GeneratedSmallEnum : byte
    {
        None,
        One,
        Two,
    }

    internal struct GeneratedMixedValue
    {
        public GeneratedNestedValue Nested;
        public GeneratedSmallEnum Mode;
        public char Symbol;
        public float Weight;
        public byte @class;
    }

    [InternalBufferCapacity(0)]
    internal struct DynamicUntypedBufferTestsBuffer : IDynamicUntypedBuffer
    {
        byte IDynamicUntypedBuffer.Value { get; }
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
    internal struct DynamicVariableMap2TestsLongKeyShortValueBuffer :
        IDynamicVariableMap<long, short, short, MultiHashColumn<short>, byte, MultiHashColumn<byte>>
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

    [GhostDynamicHashMap(SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapTestsBuffer : IDynamicMultiHashMap<int, byte>
    {
        byte IDynamicMultiHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap(
        CodecMode = GhostDynamicHashMapCodecMode.RawStable,
        SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapRawStableModeTestsBuffer : IDynamicMultiHashMap<int, byte>
    {
        byte IDynamicMultiHashMap<int, byte>.Value { get; }
    }

    [GhostDynamicHashMap(
        CodecMode = GhostDynamicHashMapCodecMode.RawStable,
        SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapRawStableObjectIdTestsBuffer : IDynamicMultiHashMap<GeneratedStableKey, BLId>
    {
        byte IDynamicMultiHashMap<GeneratedStableKey, BLId>.Value { get; }
    }

    [GhostDynamicHashMap(SendDataForChildEntity = true)]
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapGeneratedStructTestsBuffer : IDynamicMultiHashMap<GeneratedPaddedKey, GeneratedMixedValue>
    {
        byte IDynamicMultiHashMap<GeneratedPaddedKey, GeneratedMixedValue>.Value { get; }
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
