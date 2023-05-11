// <copyright file="ArchetypeChunkExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public struct FakeDynamicComponentTypeHandle // TODO remove huge hack/workaround
    {
        public TypeIndex TypeIndex;
        public short TypeLookupCache;

        public static implicit operator FakeDynamicComponentTypeHandle(DynamicComponentTypeHandle typeHandle)
        {
            return new FakeDynamicComponentTypeHandle { TypeIndex = typeHandle.m_TypeIndex, TypeLookupCache = typeHandle.m_TypeLookupCache };
        }
    }

    public static unsafe class ArchetypeChunkExtensions
    {
        public static bool DidChange(this ArchetypeChunk chunk, TypeIndex typeIndex, ref short typeLookupCache, uint version)
        {
            ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, typeIndex, ref typeLookupCache);
            int typeIndexInArchetype = typeLookupCache;
            var changeVersion = Hint.Unlikely(typeIndexInArchetype == -1) ? 0 : chunk.m_Chunk->GetChangeVersion(typeIndexInArchetype);
            return ChangeVersionUtility.DidChange(changeVersion, version);
        }

        /// <summary> Gets a read copy from the ComponentTypeHandle that doesn't trigger Change version. </summary>
        /// <param name="archetypeChunk"> The ArchetypeChunk. </param>
        /// <param name="typeHandle"> The components <see cref="ComponentTypeHandle{T}" />. THis can be read or write permission. </param>
        /// <typeparam name="T"> The <see cref="IComponentData" /> type. </typeparam>
        /// <returns> A readonly array of the component data in the chunk. </returns>
        public static NativeArray<T>.ReadOnly GetNativeArrayReadOnly<T>(
            this ArchetypeChunk archetypeChunk,
            ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData
        {
            CheckZeroSizedComponentData(typeHandle);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(typeHandle.m_Safety);
#endif
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetypeChunk.m_Chunk->Archetype, typeHandle.m_TypeIndex);
            if (typeIndexInArchetype == -1)
            {
                var emptyResult =
                    NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(null, 0, 0);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref emptyResult, typeHandle.m_Safety);
#endif
                return emptyResult.AsReadOnly();
            }

            var ptr = ChunkDataUtility.GetComponentDataRO(archetypeChunk.m_Chunk, 0, typeIndexInArchetype);

            var result = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, archetypeChunk.Count, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref result, typeHandle.m_Safety);
#endif

            return result.AsReadOnly();
        }

        public static BufferAccessor<T> GetBufferAccessorRO<T>(this ArchetypeChunk archetypeChunk, ref BufferTypeHandle<T> bufferTypeHandle)
            where T : unmanaged, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(bufferTypeHandle.m_Safety0);
#endif
            var archetype = archetypeChunk.m_Chunk->Archetype;
            var typeIndex = bufferTypeHandle.m_TypeIndex;
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            if (typeIndexInArchetype == -1)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return new BufferAccessor<T>(null, 0, 0, true, bufferTypeHandle.m_Safety0, bufferTypeHandle.m_Safety1, 0);
#else
                return new BufferAccessor<T>(null, 0, 0, 0);
#endif
            }

            var internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];

            var ptr = ChunkDataUtility.GetComponentDataRO(archetypeChunk.m_Chunk, 0, typeIndexInArchetype);

            var length = archetypeChunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new BufferAccessor<T>(
                ptr, length, stride, bufferTypeHandle.IsReadOnly, bufferTypeHandle.m_Safety0, bufferTypeHandle.m_Safety1, internalCapacity);
#else
            return new BufferAccessor<T>(ptr, length, stride, internalCapacity);
#endif
        }

        public static DynamicBufferAccessor GetDynamicBufferAccessor(this ArchetypeChunk chunk, ref DynamicComponentTypeHandle chunkBufferTypeHandle)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(chunkBufferTypeHandle.m_Safety0);
#endif
            var archetype = chunk.m_Chunk->Archetype;
            var typeIndexInArchetype = chunkBufferTypeHandle.m_TypeLookupCache;
            ChunkDataUtility.GetIndexInTypeArray(chunk.m_Chunk->Archetype, chunkBufferTypeHandle.m_TypeIndex, ref typeIndexInArchetype);
            chunkBufferTypeHandle.m_TypeLookupCache = typeIndexInArchetype;
            if (typeIndexInArchetype == -1)
            {
                return default;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (!archetype->Types[typeIndexInArchetype].IsBuffer)
            {
                throw new ArgumentException("ArchetypeChunk.GetUntypedBufferAccessor must be called only for IBufferElementData types");
            }
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(chunkBufferTypeHandle.m_Safety1);
#endif
            var internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];
            var typeInfo = TypeManager.GetTypeInfo(chunkBufferTypeHandle.m_TypeIndex);
            var ptr = chunkBufferTypeHandle.IsReadOnly
                ? ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, 0, typeIndexInArchetype, chunkBufferTypeHandle.GlobalSystemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var elementSize = typeInfo.ElementSize;

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0) && !chunkBufferTypeHandle.IsReadOnly)
            {
                chunk.JournalAddRecord(EntitiesJournaling.RecordType.GetBufferRW, chunkBufferTypeHandle.m_TypeIndex, chunkBufferTypeHandle.m_GlobalSystemVersion);
            }
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBufferAccessor(
                ptr,
                length,
                stride,
                elementSize,
                internalCapacity,
                chunkBufferTypeHandle.IsReadOnly,
                chunkBufferTypeHandle.m_Safety0,
                chunkBufferTypeHandle.m_Safety1);
#else
            return new DynamicBufferAccessor(ptr, length, stride, elementSize, internalCapacity);
#endif
        }

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void JournalAddRecord(
            this ref ArchetypeChunk chunk,
            EntitiesJournaling.RecordType recordType,
            TypeIndex typeIndex,
            uint globalSystemVersion,
            void* data = null,
            int dataLength = 0)
        {
            fixed (ArchetypeChunk* archetypeChunk = &chunk)
            {
                EntitiesJournaling.AddRecord(
                    recordType: recordType,
                    entityComponentStore: archetypeChunk->m_EntityComponentStore,
                    globalSystemVersion: globalSystemVersion,
                    chunks: archetypeChunk,
                    chunkCount: 1,
                    types: &typeIndex,
                    typeCount: 1,
                    data: data,
                    dataLength: dataLength);
            }
        }
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckZeroSizedComponentData<T>(in ComponentTypeHandle<T> chunkComponentType)
        {
            if (chunkComponentType.IsZeroSized)
            {
                throw new ArgumentException($"ArchetypeChunk.GetNativeArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
            }
        }
    }
}
