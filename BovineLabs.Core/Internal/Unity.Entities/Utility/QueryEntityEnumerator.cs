namespace BovineLabs.Core.Utility
{
    using Unity.Burst.Intrinsics;
    using Unity.Entities;

    public unsafe struct QueryEntityEnumerator
    {
        private UnsafeChunkCacheIterator _chunkCacheIterator;
        private int _chunkIndex;
        private v128 _chunkEnabledMask;

        public QueryEntityEnumerator(EntityQuery query)
        {
            _chunkIndex = -1;
            _chunkEnabledMask = default;

            var queryImpl = query._GetImpl();
            _chunkCacheIterator = new UnsafeChunkCacheIterator(queryImpl->_Filter, queryImpl->_QueryData->HasEnableableComponents != 0,
                queryImpl->GetMatchingChunkCache(), queryImpl->_QueryData->MatchingArchetypes.Ptr);
        }

        public bool MoveNextChunk(out ArchetypeChunk chunk, out ChunkEntityEnumerator chunkEnumerator)
        {
            var result = _chunkCacheIterator.MoveNextChunk(ref _chunkIndex, out chunk, out _, out var useEnabledMaskBit, ref _chunkEnabledMask);
            chunkEnumerator = new ChunkEntityEnumerator(useEnabledMaskBit != 0, _chunkEnabledMask, chunk.Count);
            return result;
        }

        public void Reset()
        {
            _chunkIndex = -1;
        }
    }
}
