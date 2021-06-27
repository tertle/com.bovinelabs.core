namespace BovineLabs.Core.Internal
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct ArrayInternals
    {
        public void* Buffer;
        public int Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public AtomicSafetyHandle Safety;
#endif
    }

    public static class ArchetypeChunkInternals
    {
        public static unsafe ArrayInternals ArrayInternals<T>(this ArchetypeChunk chunk, ComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : unmanaged, IComponentData
        {
            CheckZeroSizedComponentData(chunkComponentTypeHandle);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(chunkComponentTypeHandle.m_Safety);
#endif
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, chunkComponentTypeHandle.m_TypeIndex);

            void* buffer = null;
            int length = 0;

            if (typeIndexInArchetype != -1)
            {
                byte* ptr = chunkComponentTypeHandle.IsReadOnly
                    ? ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, 0, typeIndexInArchetype)
                    : ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, 0, typeIndexInArchetype, chunkComponentTypeHandle.GlobalSystemVersion);
                var archetype = chunk.m_Chunk->Archetype;
                length = chunk.Count;
                var batchStartOffset = chunk.m_BatchStartEntityIndex * archetype->SizeOfs[typeIndexInArchetype];
                buffer = ptr + batchStartOffset;
            }

            var result = new ArrayInternals
            {
                Buffer = buffer,
                Length = length,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Safety = chunkComponentTypeHandle.m_Safety,
#endif
            };

            return result;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckZeroSizedComponentData<T>(ComponentTypeHandle<T> chunkComponentType)
        {
            if (chunkComponentType.m_IsZeroSized)
            {
                throw new ArgumentException($"ArchetypeChunk.GetNativeArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
        }
    }
}