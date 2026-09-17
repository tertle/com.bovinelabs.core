#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections;

    internal static unsafe class DynamicHashMapNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        internal const int SnapshotSize =
            DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.SnapshotSize;
        internal const int ChangeMaskBits =
            DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.ChangeMaskBits;

        internal static bool TryPack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, out DynamicHashMapCompactHeader header)
        {
            return DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.TryPack(
                source, destination, destinationBytes, out header);
        }

        internal static bool TryGetPayloadBytes(byte* payload, int availableBytes, out int payloadBytes)
        {
            return DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.TryGetPayloadBytes(
                payload, availableBytes, out payloadBytes);
        }

        internal static bool TryRebuild(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            return DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.TryRebuild(
                targetBuffer, targetBufferLength, payload, availableBytes);
        }

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.WritePayload(
                payload, payloadBytes, ref writer);
        }

        internal static void DeserializeChunk(IntPtr snapshotData, ref DataStreamReader reader, int startOffset)
        {
            DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.DeserializeChunk(
                snapshotData, ref reader, startOffset);
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.GetDynamicDataChangeMaskSize(
                changeMaskBits, length);
        }

        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return DynamicHashCollectionNetcodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec, UniqueHashMapPolicy>.GetDynamicSnapshotSize(
                changeMaskBits, length);
        }
    }
}
#endif
