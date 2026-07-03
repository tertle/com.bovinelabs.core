// <copyright file="DynamicGeneratorTestBuffers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Iterators
{
    using System;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.Iterators.Columns;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

#if UNITY_NETCODE
    [GhostDynamicHashMap(IsDefault = true, DisplayName = "DynamicHashMap Tests Generated Compact", SendDataForChildEntity = true)]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(
        CodecMode = GhostDynamicHashMapCodecMode.RawStable,
        DisplayName = "DynamicHashMap Tests Raw Stable",
        SendDataForChildEntity = true)]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapRawStableModeTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(
        IsDefault = true,
        DisplayName = "DynamicHashMap Tests Root Only")]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapRootOnlyTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(
        IsDefault = true,
        DisplayName = "DynamicHashMap Tests Predicted Owner",
        PrefabType = GhostPrefabType.PredictedClient,
        SendTypeOptimization = GhostSendType.OnlyPredictedClients,
        OwnerSendType = SendToOwnerType.SendToOwner,
        SendDataForChildEntity = true)]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapPredictedOwnerTestsBuffer : IDynamicHashMap<int, byte>
    {
        byte IDynamicHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(
        IsDefault = true,
        DisplayName = "DynamicHashMap Tests Interpolated Non Owner",
        PrefabType = GhostPrefabType.InterpolatedClient,
        SendTypeOptimization = GhostSendType.OnlyInterpolatedClients,
        OwnerSendType = SendToOwnerType.SendToNonOwner,
        SendDataForChildEntity = true)]
#endif
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

#if UNITY_NETCODE
    [GhostDynamicHashMap(IsDefault = true, DisplayName = "DynamicHashMap Tests Generated Struct")]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicHashMapGeneratedStructTestsBuffer : IDynamicHashMap<GeneratedPaddedKey, GeneratedMixedValue>
    {
        byte IDynamicHashMap<GeneratedPaddedKey, GeneratedMixedValue>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(IsDefault = true, DisplayName = "DynamicHashMap Tests Generated Padding")]
#endif
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

#if UNITY_NETCODE
    [GhostDynamicHashMap(IsDefault = true, DisplayName = "DynamicMultiHashMap Tests Generated Compact", SendDataForChildEntity = true)]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapTestsBuffer : IDynamicMultiHashMap<int, byte>
    {
        byte IDynamicMultiHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(
        CodecMode = GhostDynamicHashMapCodecMode.RawStable,
        DisplayName = "DynamicMultiHashMap Tests Raw Stable",
        SendDataForChildEntity = true)]
#endif
    [InternalBufferCapacity(0)]
    internal struct DynamicMultiHashMapRawStableModeTestsBuffer : IDynamicMultiHashMap<int, byte>
    {
        byte IDynamicMultiHashMap<int, byte>.Value { get; }
    }

#if UNITY_NETCODE
    [GhostDynamicHashMap(IsDefault = true, DisplayName = "DynamicMultiHashMap Tests Generated Struct", SendDataForChildEntity = true)]
#endif
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
