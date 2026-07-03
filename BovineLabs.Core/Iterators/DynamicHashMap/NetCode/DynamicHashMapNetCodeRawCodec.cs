// <copyright file="DynamicHashMapNetCodeRawCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;
    using Unity.NetCode.LowLevel.Unsafe;

    internal static unsafe class DynamicHashMapNetCodeRawCodec<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal const int SnapshotSize = DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits = DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryPack(source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.WritePayload(payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.GetDynamicSnapshotSize(changeMaskBits, length);
        }
    }
}
#endif
