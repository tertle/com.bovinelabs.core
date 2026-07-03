// <copyright file="DynamicHashMapRawGhostCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;

    public readonly struct DynamicHashMapRawGhostCodec<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public const int Version = 1;
        public const int ScratchStride = DynamicHashMapNetCodeRawCodec<TKey, TValue>.SnapshotSize;
    }
}
#endif
