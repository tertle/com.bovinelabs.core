// <copyright file="DynamicHashMapNetCodeSerializer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.NetCode.LowLevel.Unsafe;

    [BurstCompile]
    public static unsafe class DynamicHashMapNetCodeSerializer<TBuffer, TKey, TValue>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private const string DefaultDisplayName = "DynamicHashMap Raw Compact";

        public static void AddToCollection(
            ref GhostComponentSerializerCollectionData collectionData, ref SystemState systemState, FixedString64Bytes displayName = default)
        {
            AddToCollection(
                ref collectionData, ref systemState, displayName, 0, null, GhostPrefabType.All, GhostSendType.AllClients, 1, SendToOwnerType.All, 0);
        }

        public static void AddToCollection(
            ref GhostComponentSerializerCollectionData collectionData, ref SystemState systemState, FixedString64Bytes displayName, ulong variantHash,
            string codecTypeFullName, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities, SendToOwnerType sendToOwner,
            byte isDefaultSerializer)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.AddToCollection(
                ref collectionData,
                ref systemState,
                GetDisplayName(displayName),
                variantHash,
                codecTypeFullName ?? typeof(DynamicHashMapRawGhostCodec<TKey, TValue>).FullName,
                prefabType,
                sendTypeOptimization,
                sendForChildEntities,
                sendToOwner,
                isDefaultSerializer);
        }

        public static ComponentTypeSerializationStrategy CreateSerializationStrategy(FixedString64Bytes displayName = default)
        {
            return CreateSerializationStrategy(displayName, 0, GhostPrefabType.All, GhostSendType.AllClients, 1, 0);
        }

        public static ComponentTypeSerializationStrategy CreateSerializationStrategy(
            FixedString64Bytes displayName, ulong variantHash, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities,
            byte isDefaultSerializer)
        {
            return DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
                .CreateSerializationStrategy(GetDisplayName(displayName), variantHash, prefabType, sendTypeOptimization, sendForChildEntities,
                    isDefaultSerializer);
        }

        public static GhostComponentSerializer.State GetState(ref SystemState systemState)
        {
            return GetState(ref systemState, 0, null, GhostPrefabType.All, GhostSendType.AllClients, SendToOwnerType.All);
        }

        public static GhostComponentSerializer.State GetState(
            ref SystemState systemState,
            ulong variantHash,
            string codecTypeFullName,
            GhostPrefabType prefabType,
            GhostSendType sendMask,
            SendToOwnerType sendToOwner)
        {
            return DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.GetState(
                ref systemState,
                GetDisplayName(default),
                variantHash,
                codecTypeFullName ?? typeof(DynamicHashMapRawGhostCodec<TKey, TValue>).FullName,
                prefabType,
                sendMask,
                sendToOwner);
        }

        public static void CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.CopyToSnapshot(
                stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        public static void CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.CopyFromSnapshot(
                stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        internal static void Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.Deserialize(
                snapshotData, baselineData, ref reader, ref compressionModel, changeMaskData, startOffset);
        }

        internal static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
                .RestoreFromBackup(componentData, backupData);
        }

        internal static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.PredictDelta(
                snapshotData, baseline1Data, baseline2Data, ref predictor);
        }

        internal static void SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            ref int snapshotDynamicDataOffset, IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.SerializeBuffer(
                stateData, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, componentData, componentDataLen, count,
                baselines, ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, ref snapshotDynamicDataOffset,
                dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        internal static void PostSerializeBuffer(
            IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int count, IntPtr baselines,
            ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
                .PostSerializeBuffer(
                    snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, count, baselines, ref writer,
                    ref compressionModel, entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        private static FixedString64Bytes GetDisplayName(FixedString64Bytes displayName)
        {
            return displayName.IsEmpty ? new FixedString64Bytes(DefaultDisplayName) : displayName;
        }
    }

    [BurstCompile]
    public static unsafe class DynamicHashMapNetCodeSerializer<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        private const string DefaultDisplayName = "DynamicHashMap Generated Compact";

        public static void AddToCollection(
            ref GhostComponentSerializerCollectionData collectionData, ref SystemState systemState, FixedString64Bytes displayName = default)
        {
            AddToCollection(
                ref collectionData, ref systemState, displayName, 0, null, GhostPrefabType.All, GhostSendType.AllClients, 1, SendToOwnerType.All, 0);
        }

        public static void AddToCollection(
            ref GhostComponentSerializerCollectionData collectionData, ref SystemState systemState, FixedString64Bytes displayName, ulong variantHash,
            string codecTypeFullName, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities, SendToOwnerType sendToOwner,
            byte isDefaultSerializer)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.AddToCollection(
                    ref collectionData,
                    ref systemState,
                    GetDisplayName(displayName),
                    variantHash,
                    codecTypeFullName ?? typeof(DynamicHashMapGeneratedGhostCodec<TKey, TValue, TKeyCodec, TValueCodec>).FullName,
                    prefabType,
                    sendTypeOptimization,
                    sendForChildEntities,
                    sendToOwner,
                    isDefaultSerializer);
        }

        public static ComponentTypeSerializationStrategy CreateSerializationStrategy(FixedString64Bytes displayName = default)
        {
            return CreateSerializationStrategy(displayName, 0, GhostPrefabType.All, GhostSendType.AllClients, 1, 0);
        }

        public static ComponentTypeSerializationStrategy CreateSerializationStrategy(
            FixedString64Bytes displayName, ulong variantHash, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities,
            byte isDefaultSerializer)
        {
            return DynamicHashCollectionNetCodeSerializerCore<
                    TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
                .CreateSerializationStrategy(GetDisplayName(displayName), variantHash, prefabType, sendTypeOptimization, sendForChildEntities,
                    isDefaultSerializer);
        }

        public static GhostComponentSerializer.State GetState(ref SystemState systemState)
        {
            return GetState(ref systemState, 0, null, GhostPrefabType.All, GhostSendType.AllClients, SendToOwnerType.All);
        }

        public static GhostComponentSerializer.State GetState(
            ref SystemState systemState,
            ulong variantHash,
            string codecTypeFullName,
            GhostPrefabType prefabType,
            GhostSendType sendMask,
            SendToOwnerType sendToOwner)
        {
            return DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.GetState(
                    ref systemState,
                    GetDisplayName(default),
                    variantHash,
                    codecTypeFullName ?? typeof(DynamicHashMapGeneratedGhostCodec<TKey, TValue, TKeyCodec, TValueCodec>).FullName,
                    prefabType,
                    sendMask,
                    sendToOwner);
        }

        public static void CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.CopyToSnapshot(
                    stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        public static void CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.CopyFromSnapshot(
                    stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        internal static void Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.Deserialize(
                    snapshotData, baselineData, ref reader, ref compressionModel, changeMaskData, startOffset);
        }

        internal static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                    TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
                .RestoreFromBackup(componentData, backupData);
        }

        internal static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.PredictDelta(
                    snapshotData, baseline1Data, baseline2Data, ref predictor);
        }

        internal static void SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            ref int snapshotDynamicDataOffset, IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.SerializeBuffer(
                    stateData, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, componentData, componentDataLen, count,
                    baselines, ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, ref snapshotDynamicDataOffset,
                    dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        internal static void PostSerializeBuffer(
            IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int count, IntPtr baselines,
            ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                    TBuffer, TKey, TValue, DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
                .PostSerializeBuffer(
                    snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, count, baselines, ref writer,
                    ref compressionModel, entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        private static FixedString64Bytes GetDisplayName(FixedString64Bytes displayName)
        {
            return displayName.IsEmpty ? new FixedString64Bytes(DefaultDisplayName) : displayName;
        }
    }

    internal readonly unsafe struct DynamicHashMapRawSerializerCodec<TBuffer, TKey, TValue> : IDynamicHashCollectionSerializerCodec<TKey>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private const string VariantTypeFullName = "BovineLabs.Core.Iterators.DynamicHashMapNetCodeRawCompactVariant";
        private const string SerializerTypeFullName = "BovineLabs.Core.Iterators.DynamicHashMapNetCodeSerializer";
        private const string GhostFieldsFormatName = "BovineLabs.Core.Iterators.DynamicHashMapRawCompactPayload.v1";

        public int SnapshotSize => DynamicHashMapNetCodeRawCodec<TKey, TValue>.SnapshotSize;

        public int ChangeMaskBits => DynamicHashMapNetCodeRawCodec<TKey, TValue>.ChangeMaskBits;

        public bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashMapNetCodeRawCodec<TKey, TValue>.TryPack(source, destination, destinationBytes, out header);
        }

        public bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashMapNetCodeRawCodec<TKey, TValue>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        public bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashMapNetCodeRawCodec<TKey, TValue>.TryRebuild(targetBuffer, targetBufferLength, payload, availableBytes);
        }

        public void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashMapNetCodeRawCodec<TKey, TValue>.WritePayload(payload, payloadBytes, ref writer);
        }

        public void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashMapNetCodeRawCodec<TKey, TValue>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        public int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashMapNetCodeRawCodec<TKey, TValue>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        public int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashMapNetCodeRawCodec<TKey, TValue>.GetDynamicSnapshotSize(changeMaskBits, length);
        }

        public ulong GetVariantHash()
        {
            return GhostVariantsUtility.UncheckedVariantHashNBC(VariantTypeFullName, typeof(TBuffer).FullName);
        }

        public ulong GetSerializerHash()
        {
            var hash = DynamicGhostPrimitiveCodec.Hash64(SerializerTypeFullName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TBuffer).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            return hash;
        }

        public ulong GetVariantTypeFullNameHash()
        {
            return DynamicGhostPrimitiveCodec.Hash64(VariantTypeFullName);
        }

        public ulong GetGhostFieldsHash(string codecTypeFullName)
        {
            var hash = DynamicGhostPrimitiveCodec.Hash64(GhostFieldsFormatName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(codecTypeFullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(DynamicHashMapCompactHeader.CurrentFormatVersion));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(sizeof(TKey)));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(sizeof(TValue)));
            return hash;
        }
    }

    internal readonly unsafe struct DynamicHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec> :
        IDynamicHashCollectionSerializerCodec<TKey>
        where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        private const string VariantTypeFullName = "BovineLabs.Core.Iterators.DynamicHashMapNetCodeGeneratedCompactVariant";
        private const string SerializerTypeFullName = "BovineLabs.Core.Iterators.DynamicHashMapNetCodeGeneratedSerializer.v2";
        private const string GhostFieldsFormatName = "BovineLabs.Core.Iterators.DynamicHashMapGeneratedCompactPayload.v2";

        public int SnapshotSize => DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.SnapshotSize;

        public int ChangeMaskBits => DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.ChangeMaskBits;

        public bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryPack(source, destination, destinationBytes, out header);
        }

        public bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        public bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        public void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.WritePayload(payload, payloadBytes, ref writer);
        }

        public void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        public int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        public int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.GetDynamicSnapshotSize(changeMaskBits, length);
        }

        public ulong GetVariantHash()
        {
            return GhostVariantsUtility.UncheckedVariantHashNBC(VariantTypeFullName, typeof(TBuffer).FullName);
        }

        public ulong GetSerializerHash()
        {
            var hash = DynamicGhostPrimitiveCodec.Hash64(SerializerTypeFullName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TBuffer).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            return hash;
        }

        public ulong GetVariantTypeFullNameHash()
        {
            return DynamicGhostPrimitiveCodec.Hash64(VariantTypeFullName);
        }

        public ulong GetGhostFieldsHash(string codecTypeFullName)
        {
            var hash = DynamicGhostPrimitiveCodec.Hash64(GhostFieldsFormatName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(codecTypeFullName ?? string.Empty));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(DynamicHashMapCompactHeader.CurrentFormatVersion));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(default(TKeyCodec).EncodedSize));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapNetCodeHash.Hash64(default(TValueCodec).EncodedSize));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TKeyCodec).SchemaHash);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TValueCodec).SchemaHash);
            return hash;
        }
    }

    internal static class DynamicHashMapNetCodeHash
    {
        public static ulong Hash64(int value)
        {
            var result = 14695981039346656037UL;
            result = (((ulong)(value & 0x000000ff) >> 0) ^ result) * 1099511628211UL;
            result = (((ulong)(value & 0x0000ff00) >> 8) ^ result) * 1099511628211UL;
            result = (((ulong)(value & 0x00ff0000) >> 16) ^ result) * 1099511628211UL;
            result = (((ulong)(value & unchecked((int)0xff000000)) >> 24) ^ result) * 1099511628211UL;
            return result;
        }
    }
}
#endif
