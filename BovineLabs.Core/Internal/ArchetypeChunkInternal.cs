// <copyright file="ArchetypeChunkInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Extensions;
    using Unity.Burst.CompilerServices;
    using Unity.Entities;

    public static class ArchetypeChunkInternal
    {
        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ref ComponentTypeHandle<T> handle)
            where T : unmanaged, IComponentData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            if (Hint.Unlikely(archetype != handle.m_LookupCache.Archetype))
            {
                handle.m_LookupCache.Update(archetype, handle.m_TypeIndex);
            }

            var typeIndexInArchetype = handle.m_LookupCache.IndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex, handle.GlobalSystemVersion);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0))
            {
                var ptr = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, archetype, 0, handle.m_TypeIndex, handle.GlobalSystemVersion,
                    ref handle.m_LookupCache);

                chunk.JournalAddRecordGetComponentDataRW(ref handle, ptr, handle.m_SizeInChunk * chunk.Count);
            }
#endif
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ref ComponentTypeHandle<T> handle, uint version)
            where T : unmanaged, IComponentData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            if (Hint.Unlikely(archetype != handle.m_LookupCache.Archetype))
            {
                handle.m_LookupCache.Update(archetype, handle.m_TypeIndex);
            }

            var typeIndexInArchetype = handle.m_LookupCache.IndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex, version);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0))
            {
                var ptr = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, archetype, 0, handle.m_TypeIndex, handle.GlobalSystemVersion,
                    ref handle.m_LookupCache);

                chunk.JournalAddRecordGetComponentDataRW(ref handle, ptr, handle.m_SizeInChunk * chunk.Count);
            }
#endif
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ref BufferTypeHandle<T> handle)
            where T : unmanaged, IBufferElementData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            if (Hint.Unlikely(archetype != handle.m_LookupCache.Archetype))
            {
                handle.m_LookupCache.Update(archetype, handle.m_TypeIndex);
            }

            var typeIndexInArchetype = handle.m_LookupCache.IndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex, handle.GlobalSystemVersion);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0))
            {
                chunk.JournalAddRecord(EntitiesJournaling.RecordType.GetBufferRW, handle.m_TypeIndex, handle.m_GlobalSystemVersion);
            }
#endif
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ref BufferTypeHandle<T> handle, uint version)
            where T : unmanaged, IBufferElementData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            if (Hint.Unlikely(archetype != handle.m_LookupCache.Archetype))
            {
                handle.m_LookupCache.Update(archetype, handle.m_TypeIndex);
            }

            var typeIndexInArchetype = handle.m_LookupCache.IndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex, version);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0))
            {
                chunk.JournalAddRecord(EntitiesJournaling.RecordType.GetBufferRW, handle.m_TypeIndex, handle.m_GlobalSystemVersion);
            }
#endif
        }

        public static unsafe void SetChangeFilter(this ArchetypeChunk chunk, ref DynamicComponentTypeHandle handle)
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, handle.m_TypeIndex);
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex, handle.GlobalSystemVersion);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0))
            {
                var ptr = ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, archetype, 0, typeIndexInArchetype, handle.GlobalSystemVersion);
                var typeSize = archetype->SizeOfs[typeIndexInArchetype];
                var length = chunk.Count;
                var byteLen = length * typeSize;

                chunk.JournalAddRecord(EntitiesJournaling.RecordType.GetComponentDataRW, handle.m_TypeIndex, handle.m_GlobalSystemVersion, ptr, byteLen);
            }
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void SetChangeFilterCheckWriteAndThrow<T>(ComponentTypeHandle<T> chunkComponentType)
            where T : IComponentData
        {
            if (chunkComponentType.IsReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only type handle");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void SetChangeFilterCheckWriteAndThrow<T>(BufferTypeHandle<T> chunkComponentType)
            where T : unmanaged, IBufferElementData
        {
            if (chunkComponentType.IsReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only type handle");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void SetChangeFilterCheckWriteAndThrow(DynamicComponentTypeHandle chunkComponentType)
        {
            if (chunkComponentType.IsReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only type handle");
            }
        }
    }
}
