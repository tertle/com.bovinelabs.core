namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class ChunkInternals
    {
        public static int GetChunkBufferSize()
        {
            return Chunk.kChunkBufferSize;
        }
    }
}
