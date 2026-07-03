// <copyright file="DynamicMultiHashMapNetCodeGeneratedCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;
    using Unity.NetCode.LowLevel.Unsafe;

    internal static unsafe class DynamicMultiHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        internal const int SnapshotSize =
            DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits =
            DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.TryPack(
                source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.TryGetPayloadBytes(
                payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.WritePayload(
                payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.DeserializeChunk(
                snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.GetDynamicDataChangeMaskSize(
                changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, MultiHashMapPolicy>.GetDynamicSnapshotSize(
                changeMaskBits, length);
        }
    }
}
#endif
