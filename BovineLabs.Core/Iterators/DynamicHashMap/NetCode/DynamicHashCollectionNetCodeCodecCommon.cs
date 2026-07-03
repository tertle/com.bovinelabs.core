// <copyright file="DynamicHashCollectionNetCodeCodecCommon.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.NetCode.LowLevel.Unsafe;

    internal static unsafe class DynamicHashCollectionNetCodeCodecCommon
    {
        internal const int SnapshotSize = 1;
        internal const int ChangeMaskBits = 1;

        internal static void WritePayload(byte* payload, int payloadBytes, ref DataStreamWriter writer)
        {
            for (var i = 0; i < payloadBytes; i++)
            {
                writer.WriteRawBits(payload[i], 8);
            }
        }

        internal static int GetDynamicDataChangeMaskSize(int changeMaskBits, int length)
        {
            return GhostComponentSerializer.SnapshotSizeAligned(GhostComponentSerializer.ChangeMaskArraySizeInUInts(changeMaskBits * length) * sizeof(uint));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetDynamicSnapshotSize(int changeMaskBits, int length)
        {
            return GhostComponentSerializer.SnapshotSizeAligned(GetDynamicDataChangeMaskSize(changeMaskBits, length) + (length * SnapshotSize));
        }
    }
}
#endif
