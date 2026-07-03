// <copyright file="DynamicHashCollectionNetCodeGeneratedCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.NetCode.LowLevel.Unsafe;

    internal static unsafe class DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, TCollectionPolicy>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
        where TCollectionPolicy : unmanaged, IDynamicHashCollectionPolicy
    {
        internal const int SnapshotSize = DynamicHashCollectionNetCodeCodecCommon.SnapshotSize;
        internal const int ChangeMaskBits = DynamicHashCollectionNetCodeCodecCommon.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            header = default;

            if (source == null || destination == null || source->SizeOfTValue != sizeof(TValue) ||
                !DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.TryCalculatePayloadBytes(source->Count, out var payloadBytes) ||
                destinationBytes < payloadBytes)
            {
                return false;
            }

            header = DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.Pack(
                source, destination, destinationBytes, default(TCollectionPolicy).TraversalOrder);
            if (destinationBytes > header.PayloadBytes)
            {
                UnsafeUtility.MemClear(destination + header.PayloadBytes, destinationBytes - header.PayloadBytes);
            }

            return true;
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            payloadBytes = 0;

            if (!DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.TryReadHeader(payload, availableBytes, out var header) ||
                !DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.TryValidateHeader(header, availableBytes))
            {
                return false;
            }

            payloadBytes = (int)header.PayloadBytes;
            return true;
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            if (targetBuffer == null || !DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.TryGetTargetDataSize(
                    payload, availableBytes, out var targetSize) ||
                targetSize != targetBufferLength)
            {
                return false;
            }

            var policy = default(TCollectionPolicy);
            DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.RebuildFromPayload(
                targetBuffer, targetBufferLength, payload, availableBytes, policy.RebuildOrder, policy.DuplicatePolicy);
            return true;
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetCodeCodecCommon.WritePayload(payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            var chunkIndex = startOffset / ChangeMaskBits;
            var chunk = (byte*)snapshotData;
            var payload = chunk - (chunkIndex * SnapshotSize);

            if (chunkIndex < DynamicHashMapCompactHeader.Size)
            {
                *chunk = (byte)reader.ReadRawBits(8);
                return;
            }

            var header = DynamicHashMapCompactHeader.Read(payload);
            CheckValidDeserializedHeader(header, chunkIndex);

            if (chunkIndex < header.PayloadBytes)
            {
                *chunk = (byte)reader.ReadRawBits(8);
            }
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeCodecCommon.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeCodecCommon.GetDynamicSnapshotSize(changeMaskBits, length);
        }

        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [System.Diagnostics.Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckValidDeserializedHeader(DynamicHashMapCompactHeader header, int chunkIndex)
        {
            if (!header.IsCurrentFormat || chunkIndex >= header.PayloadBytes)
            {
                return;
            }

            if (!DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>.TryValidateHeader(header, (int)header.PayloadBytes))
            {
                throw new InvalidOperationException("Invalid dynamic hash collection compact payload header while deserializing a NetCode chunk.");
            }
        }
    }
}
#endif
