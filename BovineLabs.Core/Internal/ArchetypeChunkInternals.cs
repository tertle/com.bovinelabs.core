// <copyright file="ArchetypeChunkInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class ArchetypeChunkInternals
    {
        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ComponentTypeHandle<T> handle)
            where T : unmanaged, IComponentData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, handle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            chunk.m_Chunk->SetChangeVersion(typeIndexInArchetype, handle.GlobalSystemVersion);
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, ComponentTypeHandle<T> handle, uint version)
            where T : unmanaged, IComponentData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, handle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            chunk.m_Chunk->SetChangeVersion(typeIndexInArchetype, version);
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> handle)
            where T : unmanaged, IBufferElementData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, handle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            chunk.m_Chunk->SetChangeVersion(typeIndexInArchetype, handle.GlobalSystemVersion);
        }

        public static unsafe void SetChangeFilter<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> handle, uint version)
            where T : unmanaged, IBufferElementData
        {
            SetChangeFilterCheckWriteAndThrow(handle);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, handle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                return;
            }

            // This should (=S) be thread safe int writes are atomic in c#
            chunk.m_Chunk->SetChangeVersion(typeIndexInArchetype, version);
        }

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
            };

            return result;
        }

        public static unsafe BufferInternals BufferInternals<T>(this ArchetypeChunk chunk, BufferTypeHandle<T> bufferComponentTypeHandle)
            where T : unmanaged, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(bufferComponentTypeHandle.m_Safety0);
#endif
            var archetype = chunk.m_Chunk->Archetype;
            var typeIndex = bufferComponentTypeHandle.m_TypeIndex;
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            if (typeIndexInArchetype == -1)
            {
                return new BufferInternals(null, 0, 0, 0);
            }

            int internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];

            byte* ptr = bufferComponentTypeHandle.IsReadOnly
                ? ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, 0, typeIndexInArchetype, bufferComponentTypeHandle.GlobalSystemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var batchStartOffset = chunk.m_BatchStartEntityIndex * stride;

            return new BufferInternals(ptr + batchStartOffset, length, stride, internalCapacity);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckZeroSizedComponentData<T>(ComponentTypeHandle<T> chunkComponentType)
        {
            if (chunkComponentType.IsZeroSized)
            {
                throw new ArgumentException($"ArchetypeChunk.GetNativeArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void SetChangeFilterCheckWriteAndThrow<T>(ComponentTypeHandle<T> chunkComponentType)
            where T : IComponentData
        {
            if (chunkComponentType.IsReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only type handle");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void SetChangeFilterCheckWriteAndThrow<T>(BufferTypeHandle<T> chunkComponentType)
            where T : unmanaged, IBufferElementData
        {
            if (chunkComponentType.IsReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only type handle");
            }
        }
    }
}
