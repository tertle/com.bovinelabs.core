// <copyright file="IDynamicGhostValueCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    public unsafe interface IDynamicGhostValueCodec<T>
        where T : unmanaged
    {
        int EncodedSize { get; }

        ulong SchemaHash { get; }

        void Encode(ref DynamicGhostEncodeContext context, in T value, byte* destination);

        void Decode(ref DynamicGhostDecodeContext context, byte* source, out T value);
    }
}
#endif
