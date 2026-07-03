// <copyright file="DynamicHashMapGeneratedGhostCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;

    public readonly struct DynamicHashMapGeneratedGhostCodec<TKey, TValue, TKeyCodec, TValueCodec>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where TKeyCodec : unmanaged, IDynamicGhostValueCodec<TKey>
        where TValueCodec : unmanaged, IDynamicGhostValueCodec<TValue>
    {
        public const int Version = 2;
        public const int ScratchStride = DynamicHashMapNetCodeGeneratedCodec<TKey, TValue, TKeyCodec, TValueCodec>.SnapshotSize;

        public int EncodedKeySize => default(TKeyCodec).EncodedSize;

        public int EncodedValueSize => default(TValueCodec).EncodedSize;

        public int EncodedEntrySize => this.EncodedKeySize + this.EncodedValueSize;

        public ulong SchemaHash
        {
            get
            {
                var hash = DynamicGhostPrimitiveCodec.Hash64("BovineLabs.Core.Iterators.DynamicHashMapGeneratedGhostCodec.v2");
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TKey).FullName));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(TValue).FullName));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TKeyCodec).SchemaHash);
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, default(TValueCodec).SchemaHash);
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)this.EncodedKeySize);
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)this.EncodedValueSize);
                return hash;
            }
        }
    }
}
#endif
