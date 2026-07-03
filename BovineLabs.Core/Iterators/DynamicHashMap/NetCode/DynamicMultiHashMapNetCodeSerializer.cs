// <copyright file="DynamicMultiHashMapNetCodeSerializer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.NetCode.LowLevel.Unsafe;

    [BurstCompile]
    public static unsafe class DynamicMultiHashMapNetCodeSerializer<TBuffer, TKey, TValue>
        where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private const string DefaultDisplayName = "DynamicMultiHashMap Raw Compact";

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
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.AddToCollection(
                ref collectionData,
                ref systemState,
                GetDisplayName(displayName),
                variantHash,
                codecTypeFullName ?? typeof(DynamicMultiHashMapRawGhostCodec<TKey, TValue>).FullName,
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
            return DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
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
            return DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.GetState(
                ref systemState,
                GetDisplayName(default),
                variantHash,
                codecTypeFullName ?? typeof(DynamicMultiHashMapRawGhostCodec<TKey, TValue>).FullName,
                prefabType,
                sendMask,
                sendToOwner);
        }

        public static void CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.CopyToSnapshot(
                stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        public static void CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.CopyFromSnapshot(
                stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        internal static void Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.Deserialize(
                snapshotData, baselineData, ref reader, ref compressionModel, changeMaskData, startOffset);
        }

        internal static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
                .RestoreFromBackup(componentData, backupData);
        }

        internal static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.PredictDelta(
                snapshotData, baseline1Data, baseline2Data, ref predictor);
        }

        internal static void SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            ref int snapshotDynamicDataOffset, IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>.SerializeBuffer(
                stateData, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, componentData, componentDataLen, count,
                baselines, ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, ref snapshotDynamicDataOffset,
                dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        internal static void PostSerializeBuffer(
            IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int count, IntPtr baselines,
            ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue>>
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
    public static unsafe class DynamicMultiHashMapNetCodeSerializer<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>
        where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        private const string DefaultDisplayName = "DynamicMultiHashMap Generated Compact";

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
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.AddToCollection(
                    ref collectionData,
                    ref systemState,
                    GetDisplayName(displayName),
                    variantHash,
                    codecTypeFullName ?? typeof(DynamicMultiHashMapGeneratedGhostCodec<TKey, TValue, TKeyCodec, TValueCodec>).FullName,
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
                    TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
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
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.GetState(
                    ref systemState,
                    GetDisplayName(default),
                    variantHash,
                    codecTypeFullName ?? typeof(DynamicMultiHashMapGeneratedGhostCodec<TKey, TValue, TKeyCodec, TValueCodec>).FullName,
                    prefabType,
                    sendMask,
                    sendToOwner);
        }

        public static void CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.CopyToSnapshot(
                    stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        public static void CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.CopyFromSnapshot(
                    stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        internal static void Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.Deserialize(
                    snapshotData, baselineData, ref reader, ref compressionModel, changeMaskData, startOffset);
        }

        internal static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                    TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
                .RestoreFromBackup(componentData, backupData);
        }

        internal static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.PredictDelta(
                    snapshotData, baseline1Data, baseline2Data, ref predictor);
        }

        internal static void SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            ref int snapshotDynamicDataOffset, IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            DynamicHashCollectionNetCodeSerializerCore<
                TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>.SerializeBuffer(
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
                    TBuffer, TKey, TValue, DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec>>
                .PostSerializeBuffer(
                    snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, count, baselines, ref writer,
                    ref compressionModel, entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        private static FixedString64Bytes GetDisplayName(FixedString64Bytes displayName)
        {
            return displayName.IsEmpty ? new FixedString64Bytes(DefaultDisplayName) : displayName;
        }
    }

    internal unsafe interface IDynamicHashCollectionSerializerCodec<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        int SnapshotSize { get; }

        int ChangeMaskBits { get; }

        bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header);

        bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes);

        bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes);

        void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer);

        void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset);

        int GetDynamicDataChangeMaskSize(int changeMaskBits, int length);

        int GetDynamicSnapshotSize(int changeMaskBits, int length);

        ulong GetVariantHash();

        ulong GetSerializerHash();

        ulong GetVariantTypeFullNameHash();

        ulong GetGhostFieldsHash(string codecTypeFullName);
    }

    internal readonly unsafe struct DynamicMultiHashMapRawSerializerCodec<TBuffer, TKey, TValue> : IDynamicHashCollectionSerializerCodec<TKey>
        where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        private const string VariantTypeFullName = "BovineLabs.Core.Iterators.DynamicMultiHashMapNetCodeRawCompactVariant";
        private const string SerializerTypeFullName = "BovineLabs.Core.Iterators.DynamicMultiHashMapNetCodeSerializer";
        private const string GhostFieldsFormatName = "BovineLabs.Core.Iterators.DynamicMultiHashMapRawCompactPayload.v1";

        public int SnapshotSize => DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.SnapshotSize;

        public int ChangeMaskBits => DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.ChangeMaskBits;

        public bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.TryPack(source, destination, destinationBytes, out header);
        }

        public bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        public bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.TryRebuild(targetBuffer, targetBufferLength, payload, availableBytes);
        }

        public void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.WritePayload(payload, payloadBytes, ref writer);
        }

        public void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        public int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        public int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>.GetDynamicSnapshotSize(changeMaskBits, length);
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
            hash = CombineCommonGhostFieldsHash(hash, codecTypeFullName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)sizeof(TKey));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)sizeof(TValue));
            return hash;
        }

        private static ulong CombineCommonGhostFieldsHash(ulong hash, string codecTypeFullName)
        {
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(codecTypeFullName ?? string.Empty));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapCompactHeader.CurrentFormatVersion);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("CollectionKind:DynamicMultiHashMap"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("TraversalOrder:BucketChain"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("DuplicatePolicy:AllowDuplicateKeys"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("RebuildOrder:PreservePackedChainOrder"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("PreserveValueOrder:True"));
            return hash;
        }
    }

    internal readonly unsafe struct DynamicMultiHashMapGeneratedSerializerCodec<TBuffer, TKey, TValue, TKeyCodec, TValueCodec> :
        IDynamicHashCollectionSerializerCodec<TKey>
        where TBuffer : unmanaged, IDynamicMultiHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        private const string VariantTypeFullName = "BovineLabs.Core.Iterators.DynamicMultiHashMapNetCodeGeneratedCompactVariant";
        private const string SerializerTypeFullName = "BovineLabs.Core.Iterators.DynamicMultiHashMapNetCodeGeneratedSerializer";
        private const string GhostFieldsFormatName = "BovineLabs.Core.Iterators.DynamicMultiHashMapGeneratedCompactPayload.v2";

        public int SnapshotSize => DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.SnapshotSize;

        public int ChangeMaskBits => DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.ChangeMaskBits;

        public bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryPack(source, destination, destinationBytes, out header);
        }

        public bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        public bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        public void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.WritePayload(payload, payloadBytes, ref writer);
        }

        public void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        public int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        public int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.GetDynamicSnapshotSize(changeMaskBits, length);
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
            hash = CombineCommonGhostFieldsHash(hash, codecTypeFullName);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)default(TKeyCodec).EncodedSize);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)default(TValueCodec).EncodedSize);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TKeyCodec).SchemaHash);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TValueCodec).SchemaHash);
            return hash;
        }

        private static ulong CombineCommonGhostFieldsHash(ulong hash, string codecTypeFullName)
        {
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(codecTypeFullName ?? string.Empty));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicHashMapCompactHeader.CurrentFormatVersion);
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("CollectionKind:DynamicMultiHashMap"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("TraversalOrder:BucketChain"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("DuplicatePolicy:AllowDuplicateKeys"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("RebuildOrder:PreservePackedChainOrder"));
            hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("PreserveValueOrder:True"));
            return hash;
        }
    }

    [BurstCompile]
    internal static unsafe class DynamicHashCollectionNetCodeSerializerCore<TBuffer, TKey, TValue, TCodec>
        where TBuffer : unmanaged, IBufferElementData
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TCodec : unmanaged, IDynamicHashCollectionSerializerCodec<TKey>
    {
        private const int IntSize = sizeof(int);
        private const int BaselinesPerEntity = 4;

        private static bool stateInitialized;
        private static GhostComponentSerializer.State state;

        public static void AddToCollection(
            ref GhostComponentSerializerCollectionData collectionData, ref SystemState systemState, FixedString64Bytes displayName, ulong variantHash,
            string codecTypeFullName, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities, SendToOwnerType sendToOwner,
            byte isDefaultSerializer)
        {
            var strategy = CreateSerializationStrategy(displayName, variantHash, prefabType, sendTypeOptimization, sendForChildEntities, isDefaultSerializer);
            collectionData.AddSerializationStrategy(ref strategy);
            var serializerState = GetState(ref systemState, displayName, strategy.Hash, codecTypeFullName, prefabType, sendTypeOptimization, sendToOwner);
            collectionData.AddSerializer(serializerState);
        }

        public static ComponentTypeSerializationStrategy CreateSerializationStrategy(
            FixedString64Bytes displayName, ulong variantHash, GhostPrefabType prefabType, GhostSendType sendTypeOptimization, byte sendForChildEntities,
            byte isDefaultSerializer)
        {
            var codec = default(TCodec);
            if (variantHash == 0)
            {
                variantHash = codec.GetVariantHash();
            }

            return new ComponentTypeSerializationStrategy
            {
                DisplayName = displayName,
                Component = ComponentType.ReadWrite<TBuffer>(),
                Hash = variantHash,
                SelfIndex = -1,
                SerializerIndex = -1,
                PrefabType = prefabType,
                SendTypeOptimization = sendTypeOptimization,
                SendForChildEntities = sendForChildEntities,
                IsDefaultSerializer = isDefaultSerializer,
            };
        }

        public static GhostComponentSerializer.State GetState(
            ref SystemState systemState,
            FixedString64Bytes displayName,
            ulong variantHash,
            string codecTypeFullName,
            GhostPrefabType prefabType,
            GhostSendType sendMask,
            SendToOwnerType sendToOwner)
        {
            if (!stateInitialized)
            {
                var codec = default(TCodec);
                if (variantHash == 0)
                {
                    variantHash = codec.GetVariantHash();
                }

                var componentType = ComponentType.ReadWrite<TBuffer>();
                state = new GhostComponentSerializer.State
                {
                    SerializerHash = codec.GetSerializerHash(),
                    GhostFieldsHash = codec.GetGhostFieldsHash(codecTypeFullName),
                    ComponentType = componentType,
                    ComponentSize = UnsafeUtility.SizeOf<TBuffer>(),
                    SnapshotSize = codec.SnapshotSize,
                    ChangeMaskBits = codec.ChangeMaskBits,
                    PrefabType = prefabType,
                    SendMask = sendMask,
                    SendToOwner = sendToOwner,
                    VariantHash = variantHash,
                    SerializationStrategyIndex = -1,
                    SerializesEnabledBit = 0,
#if UNITY_EDITOR || NETCODE_DEBUG
                    ProfilerMarker = new Unity.Profiling.ProfilerMarker(displayName.ToString()),
                    VariantTypeFullNameHash = codec.GetVariantTypeFullNameHash(),
#endif
                };

                if (state.ComponentType.IsZeroSized)
                {
                    state.ComponentSize = 0;
                }

                stateInitialized |= SetupFunctionPointers(ref state, ref systemState);
            }

            return state;
        }

        public static void CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var codec = default(TCodec);
            var destination = (byte*)snapshotData + snapshotOffset;
            var destinationBytes = count * snapshotStride;
            var source = (DynamicHashMapHelper<TKey>*)componentData;
            CheckBufferElementStride(componentStride);

            if (!codec.TryPack(source, destination, destinationBytes, out _))
            {
                ThrowInvalidPack();
            }
        }

        public static void CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            if (count <= 0)
            {
                return;
            }

            CheckBufferElementStride(componentStride);

            var codec = default(TCodec);
            ref var dataAtTick = ref GhostComponentSerializer.TypeCast<SnapshotData.DataAtTick>(snapshotData);
            var payload = (byte*)dataAtTick.SnapshotBefore + snapshotOffset;
            var availableBytes = count * snapshotStride;

            if (!codec.TryRebuild(componentData.ToPointer(), count * componentStride, payload, availableBytes))
            {
                ThrowInvalidPayload();
            }
        }

        internal static void Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            default(TCodec).DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        internal static void RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            *(byte*)componentData = *(byte*)backupData;
        }

        internal static void PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
        }

        internal static void SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            ref int snapshotDynamicDataOffset, IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            var codec = default(TCodec);
            for (var ent = 0; ent < count; ent++)
            {
                var length = GhostComponentSerializer.TypeCast<int>(componentDataLen, ent * IntSize);
                GhostComponentSerializer.TypeCast<uint>(snapshotData + (snapshotStride * ent), snapshotOffset) = (uint)length;
                GhostComponentSerializer.TypeCast<uint>(snapshotData + (snapshotStride * ent), snapshotOffset + IntSize) = (uint)snapshotDynamicDataOffset;

                var maskSize = codec.GetDynamicDataChangeMaskSize(changeMaskBits, length);
                var dynamicSize = codec.GetDynamicSnapshotSize(changeMaskBits, length);
                CheckDynamicDataRange(snapshotDynamicDataOffset, dynamicSize, dynamicSnapshotMaxOffset);

                var payload = (byte*)snapshotDynamicDataPtr + snapshotDynamicDataOffset + maskSize;
                var payloadBytes = 0;
                if (length > 0)
                {
                    var currentComponentData = GhostComponentSerializer.TypeCast<IntPtr>(componentData, UnsafeUtility.SizeOf<IntPtr>() * ent);
                    if (!codec.TryPack((DynamicHashMapHelper<TKey>*)currentComponentData, payload, length, out var header))
                    {
                        ThrowInvalidPack();
                    }

                    payloadBytes = (int)header.PayloadBytes;
                }

                SerializePackedBuffer(
                    ent, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, length, payload, payloadBytes, baselines,
                    ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, snapshotDynamicDataOffset, maskSize,
                    dynamicSize);

                snapshotDynamicDataOffset += dynamicSize;
            }
        }

        internal static void PostSerializeBuffer(
            IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int count, IntPtr baselines,
            ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            var codec = default(TCodec);
            for (var ent = 0; ent < count; ent++)
            {
                var entitySnapshot = snapshotData + (snapshotStride * ent);
                var length = (int)GhostComponentSerializer.TypeCast<uint>(entitySnapshot, snapshotOffset);
                var dynamicSnapshotDataOffset = (int)GhostComponentSerializer.TypeCast<uint>(entitySnapshot, snapshotOffset + IntSize);
                var maskSize = codec.GetDynamicDataChangeMaskSize(changeMaskBits, length);
                var dynamicSize = codec.GetDynamicSnapshotSize(changeMaskBits, length);
                CheckDynamicDataRange(dynamicSnapshotDataOffset, dynamicSize, dynamicSnapshotMaxOffset);

                var payload = (byte*)snapshotDynamicDataPtr + dynamicSnapshotDataOffset + maskSize;
                var payloadBytes = 0;
                if (length > 0 && !codec.TryGetPayloadBytes(payload, length, out payloadBytes))
                {
                    ThrowInvalidPayload();
                }

                SerializePackedBuffer(
                    ent, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, length, payload, payloadBytes, baselines,
                    ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, dynamicSnapshotDataOffset, maskSize,
                    dynamicSize);
            }
        }

        private static bool SetupFunctionPointers(ref GhostComponentSerializer.State serializerState, ref SystemState systemState)
        {
            if ((systemState.WorldUnmanaged.Flags & WorldFlags.GameServer) != WorldFlags.GameServer &&
                (systemState.WorldUnmanaged.Flags & WorldFlags.GameClient) != WorldFlags.GameClient &&
                (systemState.WorldUnmanaged.Flags & WorldFlags.GameThinClient) != WorldFlags.GameThinClient)
            {
                return false;
            }

            serializerState.PostSerializeBuffer = new PortableFunctionPointer<GhostComponentSerializer.PostSerializeBufferDelegate>(AOT_PostSerializeBuffer);
            serializerState.SerializeBuffer = new PortableFunctionPointer<GhostComponentSerializer.SerializeBufferDelegate>(AOT_SerializeBuffer);
            serializerState.CopyFromSnapshot = new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(AOT_CopyFromSnapshot);
            serializerState.CopyToSnapshot = new PortableFunctionPointer<GhostComponentSerializer.CopyToFromSnapshotDelegate>(AOT_CopyToSnapshot);
            serializerState.RestoreFromBackup = new PortableFunctionPointer<GhostComponentSerializer.RestoreFromBackupDelegate>(AOT_RestoreFromBackup);
            serializerState.PredictDelta = new PortableFunctionPointer<GhostComponentSerializer.PredictDeltaDelegate>(AOT_PredictDelta);
            serializerState.Deserialize = new PortableFunctionPointer<GhostComponentSerializer.DeserializeDelegate>(AOT_Deserialize);
#if UNITY_EDITOR || NETCODE_DEBUG
            serializerState.ReportPredictionErrors = new PortableFunctionPointer<GhostComponentSerializer.ReportPredictionErrorsDelegate>(
                AOT_ReportPredictionErrors);
#endif
            return true;
        }

        private static void SerializePackedBuffer(
            int ent, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int length, byte* payload,
            int payloadBytes, IntPtr baselines, ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit,
            IntPtr snapshotDynamicDataPtr, IntPtr dynamicSizePerEntity, int dynamicSnapshotDataOffset, int maskSize, int dynamicSize)
        {
            var baseline0Ptr = IntPtr.Zero;
            var baselineDynamicDataPtr = IntPtr.Zero;
            if (baselines != IntPtr.Zero)
            {
                var pointerSize = UnsafeUtility.SizeOf<IntPtr>();
                baseline0Ptr = GhostComponentSerializer.TypeCast<IntPtr>(baselines, pointerSize * ent * BaselinesPerEntity);
                baselineDynamicDataPtr = GhostComponentSerializer.TypeCast<IntPtr>(baselines, pointerSize * ((ent * BaselinesPerEntity) + 3));
            }

            var changeMaskPtr = snapshotData + IntSize + (ent * snapshotStride);
            ref var start = ref GhostComponentSerializer.TypeCast<int>(entityStartBit, IntSize * 2 * ent);
            start = writer.Length / IntSize;

            var codec = default(TCodec);
            var changed = !IsSameAsBaseline(baseline0Ptr, snapshotOffset, baselineDynamicDataPtr, length, payload, payloadBytes, changeMaskBits);
            var dynamicMaskBitsPtr = (byte*)snapshotDynamicDataPtr + dynamicSnapshotDataOffset;
            if (changed)
            {
                UnsafeUtility.MemSet(dynamicMaskBitsPtr, 0xff, maskSize);
                var baseLength = GetBaselineLength(baseline0Ptr, snapshotOffset);
                GhostComponentSerializer.CopyToChangeMask(changeMaskPtr, 3, maskOffsetInBits, GhostComponentSerializer.DynamicBufferComponentMaskBits);
                writer.WritePackedUIntDelta((uint)length, (uint)baseLength, compressionModel);
                codec.WritePayload(payload, payloadBytes, ref writer);
            }
            else
            {
                UnsafeUtility.MemClear(dynamicMaskBitsPtr, maskSize);
                GhostComponentSerializer.CopyToChangeMask(changeMaskPtr, 0, maskOffsetInBits, GhostComponentSerializer.DynamicBufferComponentMaskBits);
            }

            GhostComponentSerializer.TypeCast<int>(dynamicSizePerEntity, ent * IntSize) += dynamicSize;
            ref var bits = ref GhostComponentSerializer.TypeCast<int>(entityStartBit, (IntSize * 2 * ent) + IntSize);
            bits = writer.LengthInBits - (start * 32);

            var missing = (32 - writer.LengthInBits) & 31;
            if (missing > 0)
            {
                writer.WriteRawBits(0, missing);
            }
        }

        private static bool IsSameAsBaseline(
            IntPtr baseline0Ptr, int snapshotOffset, IntPtr baselineDynamicDataPtr, int length, byte* payload, int payloadBytes, int changeMaskBits)
        {
            if (baseline0Ptr == IntPtr.Zero || baselineDynamicDataPtr == IntPtr.Zero || length <= 0)
            {
                return false;
            }

            var baseLength = GetBaselineLength(baseline0Ptr, snapshotOffset);
            if (baseLength != length)
            {
                return false;
            }

            var codec = default(TCodec);
            var baseOffset = (int)GhostComponentSerializer.TypeCast<uint>(baseline0Ptr, snapshotOffset + IntSize);
            var baseMaskSize = codec.GetDynamicDataChangeMaskSize(changeMaskBits, baseLength);
            var basePayload = (byte*)baselineDynamicDataPtr + baseOffset + baseMaskSize;
            return codec.TryGetPayloadBytes(basePayload, baseLength, out var basePayloadBytes) &&
                basePayloadBytes == payloadBytes && UnsafeUtility.MemCmp(basePayload, payload, payloadBytes) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBaselineLength(IntPtr baseline0Ptr, int snapshotOffset)
        {
            return baseline0Ptr == IntPtr.Zero ? 0 : (int)GhostComponentSerializer.TypeCast<uint>(baseline0Ptr, snapshotOffset);
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckDynamicDataRange(int dynamicSnapshotDataOffset, int dynamicSize, int dynamicSnapshotMaxOffset)
        {
            if (dynamicSnapshotDataOffset + dynamicSize > dynamicSnapshotMaxOffset)
            {
                throw new InvalidOperationException("Writing dynamic hash collection NetCode snapshot data outside the dynamic snapshot buffer.");
            }
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckBufferElementStride(int componentStride)
        {
            if (componentStride != UnsafeUtility.SizeOf<TBuffer>() || componentStride != 1)
            {
                throw new InvalidOperationException("Dynamic hash collection NetCode serialization requires byte-sized buffer elements.");
            }
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowInvalidPack()
        {
            throw new InvalidOperationException("Unable to pack dynamic hash collection into the NetCode compact snapshot payload.");
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowInvalidPayload()
        {
            throw new InvalidOperationException("Invalid dynamic hash collection NetCode compact snapshot payload.");
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void AOT_CopyToSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            CopyToSnapshot(stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.CopyToFromSnapshotDelegate))]
        private static void AOT_CopyFromSnapshot(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, IntPtr componentData, int componentStride, int count)
        {
            CopyFromSnapshot(stateData, snapshotData, snapshotOffset, snapshotStride, componentData, componentStride, count);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.DeserializeDelegate))]
        private static void AOT_Deserialize(
            IntPtr snapshotData, IntPtr baselineData, ref DataStreamReader reader, ref StreamCompressionModel compressionModel, IntPtr changeMaskData,
            int startOffset)
        {
            Deserialize(snapshotData, baselineData, ref reader, ref compressionModel, changeMaskData, startOffset);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.SerializeBufferDelegate))]
        private static void AOT_SerializeBuffer(
            IntPtr stateData, IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits,
            IntPtr componentData, IntPtr componentDataLen, int count, IntPtr baselines, ref DataStreamWriter writer,
            ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr, ref int snapshotDynamicDataOffset,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            SerializeBuffer(
                stateData, snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, componentData, componentDataLen, count,
                baselines, ref writer, ref compressionModel, entityStartBit, snapshotDynamicDataPtr, ref snapshotDynamicDataOffset,
                dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.PostSerializeBufferDelegate))]
        private static void AOT_PostSerializeBuffer(
            IntPtr snapshotData, int snapshotOffset, int snapshotStride, int maskOffsetInBits, int changeMaskBits, int count, IntPtr baselines,
            ref DataStreamWriter writer, ref StreamCompressionModel compressionModel, IntPtr entityStartBit, IntPtr snapshotDynamicDataPtr,
            IntPtr dynamicSizePerEntity, int dynamicSnapshotMaxOffset)
        {
            PostSerializeBuffer(
                snapshotData, snapshotOffset, snapshotStride, maskOffsetInBits, changeMaskBits, count, baselines, ref writer, ref compressionModel,
                entityStartBit, snapshotDynamicDataPtr, dynamicSizePerEntity, dynamicSnapshotMaxOffset);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.RestoreFromBackupDelegate))]
        private static void AOT_RestoreFromBackup(IntPtr componentData, IntPtr backupData)
        {
            RestoreFromBackup(componentData, backupData);
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.PredictDeltaDelegate))]
        private static void AOT_PredictDelta(IntPtr snapshotData, IntPtr baseline1Data, IntPtr baseline2Data, ref GhostDeltaPredictor predictor)
        {
            PredictDelta(snapshotData, baseline1Data, baseline2Data, ref predictor);
        }

#if UNITY_EDITOR || NETCODE_DEBUG
        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(GhostComponentSerializer.ReportPredictionErrorsDelegate))]
        private static void AOT_ReportPredictionErrors(IntPtr componentData, IntPtr backupData, IntPtr errorsList, int errorsCount)
        {
        }
#endif
    }
}
#endif
