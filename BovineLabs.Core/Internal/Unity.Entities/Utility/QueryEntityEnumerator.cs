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
                queryImpl->GetQueryChunks());
        }

        public bool MoveNextChunk(out ArchetypeChunk chunk, out ChunkEntityEnumerator chunkEnumerator)
        {
            var result = _chunkCacheIterator.MoveNextChunk(ref _chunkIndex, out chunk, out var entityCount, out var useEnabledMask, ref _chunkEnabledMask);
            chunkEnumerator = new ChunkEntityEnumerator(useEnabledMask != 0, _chunkEnabledMask, entityCount);
            return result;
        }

        public void Reset()
        {
            _chunkIndex = -1;
        }
    }
}
