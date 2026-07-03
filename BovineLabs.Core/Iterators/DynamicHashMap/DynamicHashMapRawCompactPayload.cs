// <copyright file="DynamicHashMapRawCompactPayload.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Collections.LowLevel.Unsafe;

    internal static unsafe class DynamicHashMapRawCompactPayload<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal const int HeaderSize = DynamicHashMapCompactHeader.Size;

        internal static int CalculatePayloadBytes(int count)
        {
            Check.Assume(TryCalculatePayloadBytes(count, out var payloadBytes), "Invalid compact payload size.");
            return payloadBytes;
        }

        internal static bool TryCalculatePayloadBytes(int count, out int payloadBytes)
        {
            return TryCalculatePayloadBytes(count, sizeof(TKey), sizeof(TValue), out payloadBytes);
        }

        internal static bool TryCalculatePayloadBytes(int count, int keySize, int valueSize, out int payloadBytes)
        {
            payloadBytes = 0;

            if (count < 0 || keySize < 0 || valueSize < 0)
            {
                return false;
            }

            var entryBytes = (long)keySize + valueSize;
            var totalBytes = DynamicHashMapCompactHeader.Size + ((long)count * entryBytes);
            if (totalBytes > int.MaxValue)
            {
                return false;
            }

            payloadBytes = (int)totalBytes;
            return true;
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
            var valuesDestination = keysDestination + (source->Count * sizeof(TKey));
            source->CopyActiveEntriesTo(keysDestination, sizeof(TKey), valuesDestination, sizeof(TValue), traversalOrder);

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
                var valuesSource = keysSource + (count * sizeof(TKey));
                UnsafeUtility.MemCpy(writeView.Keys, keysSource, count * sizeof(TKey));
                UnsafeUtility.MemCpy(writeView.Values, valuesSource, count * sizeof(TValue));
            }

            DynamicHashMapHelper<TKey>.CompleteDenseRebuild(ref writeView, rebuildOrder, duplicatePolicy);
        }
    }
}
