// <copyright file="QueryEntityEnumerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    public unsafe struct QueryEntityEnumerator
    {
        private UnsafeChunkCacheIterator chunkCacheIterator;
        private int chunkIndex;
        private v128 chunkEnabledMask;

        public QueryEntityEnumerator(EntityQuery query)
        {
            this.chunkIndex = -1;
            this.chunkEnabledMask = default;

            var queryImpl = query._GetImpl();
            this.chunkCacheIterator = new UnsafeChunkCacheIterator(queryImpl->_Filter, queryImpl->_QueryData->HasEnableableComponents != 0,
                queryImpl->GetMatchingChunkCache(), queryImpl->_QueryData->MatchingArchetypes.Ptr);
        }

        public bool MoveNextChunk(out ArchetypeChunk chunk, out ChunkEntityEnumerator chunkEnumerator)
        {
            var result = this.chunkCacheIterator.MoveNextChunk(ref this.chunkIndex, out chunk, out _, out var useEnabledMaskBit, ref this.chunkEnabledMask);
            chunkEnumerator = new ChunkEntityEnumerator(useEnabledMaskBit != 0, this.chunkEnabledMask, chunk.Count);
            return result;
        }

        public void Reset()
        {
            this.chunkIndex = -1;
        }
    }
}
