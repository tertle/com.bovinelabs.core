// <copyright file="ChunkLinks.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    // Chunk component
    public unsafe struct ChunkLinks : IComponentData
    {
        public const int MaxGroupIDs = 8;

        public ArchetypeChunk Chunk0;
        public ArchetypeChunk Chunk1;
        public ArchetypeChunk Chunk2;
        public ArchetypeChunk Chunk3;
        public ArchetypeChunk Chunk4;
        public ArchetypeChunk Chunk5;
        public ArchetypeChunk Chunk6;
        public ArchetypeChunk Chunk7;

        internal ref ArchetypeChunk this[int index]
        {
            get
            {
                CheckInRange(index);
                return ref ((ArchetypeChunk*)UnsafeUtility.AddressOf(ref this))[index];
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckInRange(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= MaxGroupIDs)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
#endif
        }
    }
}
#endif
