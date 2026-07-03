// <copyright file="DynamicMultiHashMapNetCodeRawCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;
    using Unity.NetCode.LowLevel.Unsafe;

    internal static unsafe class DynamicMultiHashMapNetCodeRawCodec<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal const int SnapshotSize = DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits = DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryPack(source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.WritePayload(payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeRawCodec<TKey, TValue, MultiHashMapPolicy>.GetDynamicSnapshotSize(changeMaskBits, length);
        }
    }
}
#endif
