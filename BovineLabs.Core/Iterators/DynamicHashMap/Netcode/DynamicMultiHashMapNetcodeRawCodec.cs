#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;

    internal static unsafe class DynamicMultiHashMapNetcodeRawCodec<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal const int SnapshotSize = DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits = DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryPack(source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.WritePayload(payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, MultiHashMapPolicy>.GetDynamicSnapshotSize(changeMaskBits, length);
        }
    }
}
#endif
