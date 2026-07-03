// <copyright file="DynamicHashMapGeneratedCompactPayload.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Collections.LowLevel.Unsafe;

    internal static unsafe class DynamicHashMapGeneratedCompactPayload<TKey, TValue, TKeyCodec, TValueCodec>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        internal const int HeaderSize = DynamicHashMapCompactHeader.Size;

        internal static int EncodedKeySize => default(TKeyCodec).EncodedSize;

        internal static int EncodedValueSize => default(TValueCodec).EncodedSize;

        internal static int CalculatePayloadBytes(int count)
        {
            Check.Assume(TryCalculatePayloadBytes(count, out var payloadBytes), "Invalid compact payload size.");
            return payloadBytes;
        }

        internal static bool TryCalculatePayloadBytes(int count, out int payloadBytes)
        {
            return DynamicHashMapRawCompactPayload<TKey, TValue>.TryCalculatePayloadBytes(count, EncodedKeySize, EncodedValueSize, out payloadBytes);
        }

        internal static DynamicHashMapCompactHeader Pack(DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes)
        {
            return Pack(source, destination, destinationBytes, DynamicHashMapTraversalOrder.DenseIndex);
        }

        internal static DynamicHashMapCompactHeader Pack(
            DynamicHashMapHelper<TKey>* source, byte* destination, int destinationBytes, DynamicHashMapTraversalOrder traversalOrder)
        {
            Check.Assume(source != null, "Source map must not be null.");
            Check.Assume(destination != null, "Compact payload destination must not be null.");
            Check.Assume(source->SizeOfTValue == sizeof(TValue), "Value size does not match the source map.");

            var payloadBytes = CalculatePayloadBytes(source->Count);
            Check.Assume(destinationBytes >= payloadBytes, "Compact payload destination is too small.");

            var header = DynamicHashMapCompactHeader.Create(source->Count, source->Capacity, payloadBytes, source->Log2MinGrowth);
            header.Write(destination);

            if (source->Count == 0)
            {
                return header;
            }

            var keysDestination = destination + DynamicHashMapCompactHeader.Size;
            var valuesDestination = keysDestination + (source->Count * EncodedKeySize);
            PackEntries(source, keysDestination, valuesDestination, traversalOrder);
            return header;
        }

        internal static bool TryReadHeader(byte* payload, int availableBytes, out DynamicHashMapCompactHeader header)
        {
            header = default;

            if (payload == null || availableBytes < DynamicHashMapCompactHeader.Size)
            {
                return false;
            }

            header = DynamicHashMapCompactHeader.Read(payload);
            return true;
        }

        internal static bool TryValidateHeader(DynamicHashMapCompactHeader header, int availableBytes)
        {
            if (!header.IsCurrentFormat || availableBytes < DynamicHashMapCompactHeader.Size || header.Count > int.MaxValue ||
                header.Capacity > int.MaxValue || header.PayloadBytes > int.MaxValue || header.Count > header.Capacity || header.Capacity == 0)
            {
                return false;
            }

            if (!TryCalculatePayloadBytes((int)header.Count, out var expectedPayloadBytes) || expectedPayloadBytes != header.PayloadBytes)
            {
                return false;
            }

            return header.PayloadBytes <= availableBytes &&
                DynamicHashMapHelper<TKey>.TryCalculateDataSize((int)header.Capacity, sizeof(TValue), out _);
        }

        internal static bool TryGetTargetDataSize(byte* payload, int availableBytes, out int dataSize)
        {
            dataSize = 0;

            if (!TryReadHeader(payload, availableBytes, out var header) || !TryValidateHeader(header, availableBytes))
            {
                return false;
            }

            DynamicHashMapHelper<TKey>.TryCalculateDataSize((int)header.Capacity, sizeof(TValue), out var layout);
            dataSize = layout.TotalSize;
            return true;
        }

        internal static void RebuildFromPayload(void* targetBuffer, int targetBufferLength, byte* payload, int availableBytes)
        {
            RebuildFromPayload(
                targetBuffer, targetBufferLength, payload, availableBytes, DynamicHashMapRebuildOrder.Default,
                DynamicHashMapDuplicatePolicy.RejectDuplicateKeys);
        }

        internal static void RebuildFromPayload(
            void* targetBuffer,
            int targetBufferLength,
            byte* payload,
            int availableBytes,
            DynamicHashMapRebuildOrder rebuildOrder,
            DynamicHashMapDuplicatePolicy duplicatePolicy)
        {
            Check.Assume(targetBuffer != null, "Target map buffer must not be null.");
            Check.Assume(TryReadHeader(payload, availableBytes, out var header), "Compact payload header is missing.");
            Check.Assume(TryValidateHeader(header, availableBytes), "Compact payload header is invalid.");

            var count = (int)header.Count;
            var writeView = DynamicHashMapHelper<TKey>.BeginDenseRebuild(
                targetBuffer, targetBufferLength, count, (int)header.Capacity, sizeof(TValue), header.Log2MinGrowth);

            if (count > 0)
            {
                var keysSource = payload + DynamicHashMapCompactHeader.Size;
                var valuesSource = keysSource + (count * EncodedKeySize);
                DecodeEntries(ref writeView, keysSource, valuesSource);
            }

            DynamicHashMapHelper<TKey>.CompleteDenseRebuild(ref writeView, rebuildOrder, duplicatePolicy);
        }

        private static void PackEntries(
            DynamicHashMapHelper<TKey>* source, byte* keysDestination, byte* valuesDestination, DynamicHashMapTraversalOrder traversalOrder)
        {
            var keyCodec = default(TKeyCodec);
            var valueCodec = default(TValueCodec);
            var context = default(DynamicGhostEncodeContext);
            var packedIndex = 0;

            if (traversalOrder == DynamicHashMapTraversalOrder.DenseIndex && source->IsDense)
            {
                for (var idx = 0; idx < source->Count; idx++)
                {
                    PackEntry(source, idx, packedIndex++, ref keyCodec, ref valueCodec, ref context, keysDestination, valuesDestination);
                }

                return;
            }

            var buckets = source->Buckets;
            var next = source->Next;
            for (var bucketIndex = 0; bucketIndex < source->BucketCapacity; bucketIndex++)
            {
                for (var idx = buckets[bucketIndex]; idx != -1; idx = next[idx])
                {
                    PackEntry(source, idx, packedIndex++, ref keyCodec, ref valueCodec, ref context, keysDestination, valuesDestination);
                }
            }
        }

        private static void PackEntry(
            DynamicHashMapHelper<TKey>* source,
            int sourceIndex,
            int packedIndex,
            ref TKeyCodec keyCodec,
            ref TValueCodec valueCodec,
            ref DynamicGhostEncodeContext context,
            byte* keysDestination,
            byte* valuesDestination)
        {
            var key = UnsafeUtility.ReadArrayElement<TKey>(source->Keys, sourceIndex);
            var value = UnsafeUtility.ReadArrayElement<TValue>(source->Values, sourceIndex);
            keyCodec.Encode(ref context, in key, keysDestination + (packedIndex * EncodedKeySize));
            valueCodec.Encode(ref context, in value, valuesDestination + (packedIndex * EncodedValueSize));
        }

        private static void DecodeEntries(ref DynamicHashMapDenseWriteView<TKey> writeView, byte* keysSource, byte* valuesSource)
        {
            var keyCodec = default(TKeyCodec);
            var valueCodec = default(TValueCodec);
            var context = default(DynamicGhostDecodeContext);

            for (var idx = 0; idx < writeView.Count; idx++)
            {
                keyCodec.Decode(ref context, keysSource + (idx * EncodedKeySize), out var key);
                valueCodec.Decode(ref context, valuesSource + (idx * EncodedValueSize), out var value);
                UnsafeUtility.WriteArrayElement(writeView.Keys, idx, key);
                UnsafeUtility.WriteArrayElement(writeView.Values, idx, value);
            }
        }
    }
}
#endif
