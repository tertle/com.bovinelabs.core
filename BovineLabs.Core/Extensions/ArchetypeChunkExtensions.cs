// <copyright file="ArchetypeChunkExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static class ArchetypeChunkExtensions
    {
        /// <summary> Gets a read copy from the ComponentTypeHandle that doesn't trigger Change version. </summary>
        /// <param name="archetypeChunk"> The ArchetypeChunk. </param>
        /// <param name="chunkComponentTypeHandle"> The components <see cref="ComponentTypeHandle{T}"/>. THis can be read or write permission.</param>
        /// <typeparam name="T"> The <see cref="IComponentData"/> type. </typeparam>
        /// <returns> A readonly array of the component data in the chunk. </returns>
        public static unsafe NativeArray<T>.ReadOnly GetNativeArrayReadOnly<T>(
            this ArchetypeChunk archetypeChunk,
            ComponentTypeHandle<T> chunkComponentTypeHandle)
            where T : unmanaged, IComponentData
        {
            CheckZeroSizedComponentData(archetypeChunk, chunkComponentTypeHandle);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(chunkComponentTypeHandle.m_Safety);
#endif
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetypeChunk.m_Chunk->Archetype, chunkComponentTypeHandle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                var emptyResult =
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(null, 0, 0);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref emptyResult, chunkComponentTypeHandle.m_Safety);
#endif
                return emptyResult.AsReadOnly();
            }

            byte* ptr = ChunkDataUtility.GetComponentDataRO(archetypeChunk.m_Chunk, 0, typeIndexInArchetype);
            var archetype = archetypeChunk.m_Chunk->Archetype;
            var length = archetypeChunk.Count;
            var batchStartOffset = archetypeChunk.m_BatchStartEntityIndex * archetype->SizeOfs[typeIndexInArchetype];
            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr + batchStartOffset, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, chunkComponentTypeHandle.m_Safety);
#endif
            return result.AsReadOnly();
        }

        public static unsafe BufferAccessor<T> GetBufferAccessorRO<T>(this ArchetypeChunk archetypeChunk, BufferTypeHandle<T> bufferComponentTypeHandle)
            where T : struct, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(bufferComponentTypeHandle.m_Safety0);
#endif
            var archetype = archetypeChunk.m_Chunk->Archetype;
            var typeIndex = bufferComponentTypeHandle.m_TypeIndex;
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            if (typeIndexInArchetype == -1)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return new BufferAccessor<T>(null, 0, 0, true, bufferComponentTypeHandle.m_Safety0, bufferComponentTypeHandle.m_Safety1, 0);
#else
                return new BufferAccessor<T>(null, 0, 0, 0);
#endif
            }

            int internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];

            byte* ptr = ChunkDataUtility.GetComponentDataRO(archetypeChunk.m_Chunk, 0, typeIndexInArchetype);

            var length = archetypeChunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var batchStartOffset = archetypeChunk.m_BatchStartEntityIndex * stride;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new BufferAccessor<T>(ptr + batchStartOffset, length, stride, bufferComponentTypeHandle.IsReadOnly, bufferComponentTypeHandle.m_Safety0, bufferComponentTypeHandle.m_Safety1, internalCapacity);
#else
            return new BufferAccessor<T>(ptr + batchStartOffset, length, stride, internalCapacity);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckZeroSizedComponentData<T>(ArchetypeChunk archetypeChunk, ComponentTypeHandle<T> chunkComponentType)
        {
            if (chunkComponentType.m_IsZeroSized)
                throw new ArgumentException($"ArchetypeChunk.GetNativeArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
        }
    }
}
