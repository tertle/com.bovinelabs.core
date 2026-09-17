#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;

    internal static unsafe class DynamicHashMapNetcodeRawCodec<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal const int SnapshotSize = DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits = DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryPack(source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryGetPayloadBytes(payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.WritePayload(payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.DeserializeChunk(snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.GetDynamicDataChangeMaskSize(changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeRawCodec<TKey, TValue, UniqueHashMapPolicy>.GetDynamicSnapshotSize(changeMaskBits, length);
        }
    }
}
#endif
