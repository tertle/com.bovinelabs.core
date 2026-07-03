// <copyright file="DynamicGhostRawValueCodec.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct DynamicGhostRawValueCodec<T> : IDynamicGhostValueCodec<T>
        where T : unmanaged
    {
        public int EncodedSize => sizeof(T);

        public ulong SchemaHash
        {
            get
            {
                var hash = DynamicGhostPrimitiveCodec.Hash64("BovineLabs.Core.Iterators.DynamicGhostRawValueCodec.v1");
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64(typeof(T).FullName));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, (ulong)sizeof(T));
                return hash;
            }
        }

        public void Encode(ref DynamicGhostEncodeContext context, in T value, byte* destination)
        {
            var copy = value;
            UnsafeUtility.MemCpy(destination, &copy, sizeof(T));
        }

        public void Decode(ref DynamicGhostDecodeContext context, byte* source, out T value)
        {
            UnsafeUtility.CopyPtrToStructure(source, out value);
        }
    }
}
#endif
