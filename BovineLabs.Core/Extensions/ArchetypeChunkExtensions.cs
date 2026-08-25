// <copyright file="ArchetypeChunkExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public static unsafe class ArchetypeChunkExtensions
    {
        public static DynamicBufferAccessor GetDynamicBufferAccessor(this ArchetypeChunk chunk, ref DynamicComponentTypeHandle chunkBufferTypeHandle)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(chunkBufferTypeHandle.m_Safety0);
#endif
            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);
            var typeIndexInArchetype = chunkBufferTypeHandle.m_TypeLookupCache;
            ChunkDataUtility.GetIndexInTypeArray(archetype, chunkBufferTypeHandle.m_TypeIndex, ref typeIndexInArchetype);
            chunkBufferTypeHandle.m_TypeLookupCache = typeIndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return default;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (Hint.Unlikely(!archetype->Types[typeIndexInArchetype].IsBuffer))
            {
                throw new ArgumentException("ArchetypeChunk.GetUntypedBufferAccessor must be called only for IBufferElementData types");
            }
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Expect the safety to be set and valid
            AtomicSafetyHandle.CheckReadAndThrow(chunkBufferTypeHandle.m_Safety1);
#endif
            var internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];
            var typeInfo = TypeManager.GetTypeInfo(chunkBufferTypeHandle.m_TypeIndex);
            var ptr = chunkBufferTypeHandle.IsReadOnly
                ? ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, archetype, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, archetype, 0, typeIndexInArchetype, chunkBufferTypeHandle.GlobalSystemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var elementSize = typeInfo.ElementSize;
            var elementAlign = typeInfo.AlignmentInBytes;

#if UNITY_INCLUDE_INSTRUMENTATION && !DISABLE_ENTITIES_JOURNALING
#pragma warning disable 0618
            if (Hint.Unlikely(chunk.m_EntityComponentStore->m_RecordToJournal != 0) && !chunkBufferTypeHandle.IsReadOnly)
            {
                chunk.JournalAddRecord(EntitiesJournaling.RecordType.GetBufferRW, chunkBufferTypeHandle.m_TypeIndex,
                    chunkBufferTypeHandle.m_GlobalSystemVersion);
            }
#pragma warning restore 0618
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBufferAccessor(ptr, length, stride, elementSize, elementAlign, internalCapacity, chunkBufferTypeHandle.IsReadOnly,
                chunkBufferTypeHandle.m_Safety0, chunkBufferTypeHandle.m_Safety1);
#else
            return new DynamicBufferAccessor(ptr, length, stride, elementSize, elementAlign, internalCapacity);
#endif
        }

        public static void CopyEnableMaskFrom<TD, TS>(
            this ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<TD> destination, ref ComponentTypeHandle<TS> source)
            where TD : unmanaged, IComponentData, IEnableableComponent
            where TS : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(destination.m_Safety);
            AtomicSafetyHandle.CheckReadAndThrow(source.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            if (Hint.Unlikely(destination.m_LookupCache.Archetype != archetype))
            {
                destination.m_LookupCache.Update(archetype, destination.m_TypeIndex);
            }

            if (Hint.Unlikely(source.m_LookupCache.Archetype != archetype))
            {
                source.m_LookupCache.Update(archetype, source.m_TypeIndex);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (Hint.Unlikely(destination.m_LookupCache.IndexInArchetype == -1))
            {
                throw new InvalidOperationException(); // TODO
            }

            if (Hint.Unlikely(source.m_LookupCache.IndexInArchetype == -1))
            {
                throw new InvalidOperationException(); // TODO
            }
#endif
            var dst = ChunkDataUtility.GetEnabledRefRW(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, destination.m_LookupCache.IndexInArchetype,
                    destination.GlobalSystemVersion, out var dstPtrChunkDisabledCount)
                .Ptr;

            var src = ChunkDataUtility.GetEnabledRefRO(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, source.m_LookupCache.IndexInArchetype).Ptr;

            var chunks = archetype->Chunks;
            var memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[source.m_LookupCache.IndexInArchetype];
            var srcPtrChunkDisabledCount = chunks.GetPointerToChunkDisabledCountForType(memoryOrderIndexInArchetype, archetypeChunk.m_Chunk.ListIndex);

            dst[0] = src[0];
            dst[1] = src[1];
            *dstPtrChunkDisabledCount = *srcPtrChunkDisabledCount;
        }

        public static v128* GetEnabledBitsRO<T>(this ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(typeHandle.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            if (Hint.Unlikely(typeHandle.m_LookupCache.Archetype != archetype))
            {
                typeHandle.m_LookupCache.Update(archetype, typeHandle.m_TypeIndex);
            }

            if (Hint.Unlikely(typeHandle.m_LookupCache.IndexInArchetype == -1))
            {
                return null;
            }

            return (v128*)ChunkDataUtility
                .GetEnabledRefRO(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, typeHandle.m_LookupCache.IndexInArchetype)
                .Ptr;
        }

        public static v128* GetEnabledBitsRW<T>(this ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<T> typeHandle, out int* ptrChunkDisabledCount)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(typeHandle.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            if (Hint.Unlikely(typeHandle.m_LookupCache.Archetype != archetype))
            {
                typeHandle.m_LookupCache.Update(archetype, typeHandle.m_TypeIndex);
            }

            if (Hint.Unlikely(typeHandle.m_LookupCache.IndexInArchetype == -1))
            {
                ptrChunkDisabledCount = null;
                return null;
            }

            return (v128*)ChunkDataUtility.GetEnabledRefRW(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype,
                    typeHandle.m_LookupCache.IndexInArchetype, typeHandle.GlobalSystemVersion, out ptrChunkDisabledCount)
                .Ptr;
        }

        public static ref readonly v128 GetRequiredEnabledBitsRO<T>(this ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(typeHandle.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            if (Hint.Unlikely(typeHandle.m_LookupCache.Archetype != archetype))
            {
                typeHandle.m_LookupCache.Update(archetype, typeHandle.m_TypeIndex);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            // Must check this after computing the pointer, to make sure the cache is up to date
            if (Hint.Unlikely(typeHandle.m_LookupCache.IndexInArchetype == -1))
            {
                var typeName = typeHandle.m_TypeIndex.ToFixedString();
                throw new ArgumentException($"Required component {typeName} not found in archetype.");
            }
#endif

            var ptr = ChunkDataUtility.GetEnabledRefRO(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, typeHandle.m_LookupCache.IndexInArchetype)
                .Ptr;

            return ref UnsafeUtility.AsRef<v128>(ptr);
        }

        public static ref v128 GetRequiredEnabledBitsRW<T>(
            this in ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<T> typeHandle, out int* ptrChunkDisabledCount)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(typeHandle.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            if (Hint.Unlikely(typeHandle.m_LookupCache.Archetype != archetype))
            {
                typeHandle.m_LookupCache.Update(archetype, typeHandle.m_TypeIndex);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (Hint.Unlikely(typeHandle.IsReadOnly))
            {
                throw new InvalidOperationException("Provided ComponentTypeHandle is read-only; can't get a read/write pointer to component data");
            }

            // Must check this after computing the pointer, to make sure the cache is up to date
            if (Hint.Unlikely(typeHandle.m_LookupCache.IndexInArchetype == -1))
            {
                var typeName = typeHandle.m_TypeIndex.ToFixedString();
                throw new ArgumentException($"Required component {typeName} not found in archetype.");
            }
#endif

            var ptr = ChunkDataUtility.GetEnabledRefRW(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, typeHandle.m_LookupCache.IndexInArchetype,
                    typeHandle.GlobalSystemVersion, out ptrChunkDisabledCount)
                .Ptr;

            return ref UnsafeUtility.AsRef<v128>(ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UpdateChunkDisabledCount(this in ArchetypeChunk archetypeChunk, int* ptrChunkDisabledCount, in v128 bits)
        {
            *ptrChunkDisabledCount = archetypeChunk.Count - math.countbits(bits.ULong0) - math.countbits(bits.ULong1);
        }

        public static void GetEnableableActiveMasks(this ArchetypeChunk archetypeChunk, out ulong mask0, out ulong mask1)
        {
            var i0 = math.min(archetypeChunk.Count, 64);
            mask0 = ulong.MaxValue >> (64 - i0); // can never have no elements
            var i1 = math.max(archetypeChunk.Count - 64, 0);
            mask1 = math.select(ulong.MaxValue >> (64 - i1), 0, i1 == 0); // >> 64 does nothing by c# specification
        }

        /// <summary>
        /// Provides a ComponentEnabledMask to the component enabled bits in this chunk.
        /// </summary>
        /// <typeparam name="T"> The component type </typeparam>
        /// <param name="typeHandle"> Type handle for the component type <typeparamref name="T" />. </param>
        /// <returns> An <see cref="EnabledMask" /> instance for component <typeparamref name="T" /> in this chunk. </returns>
        public static EnabledMask GetEnabledMaskRO<T>(this ArchetypeChunk archetypeChunk, ref ComponentTypeHandle<T> typeHandle)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(typeHandle.m_Safety);
#endif
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);
            if (Hint.Unlikely(typeHandle.m_LookupCache.Archetype != archetype))
            {
                typeHandle.m_LookupCache.Update(archetype, typeHandle.m_TypeIndex);
            }

            // In case the chunk does not contains the component type (or the internal TypeIndex lookup fails to find a
            // match), the LookupCache.Update will invalidate the IndexInArchetype.
            // In such a case, we return an empty EnabledMask.
            if (Hint.Unlikely(typeHandle.m_LookupCache.IndexInArchetype == -1))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return new EnabledMask(new SafeBitRef(null, 0, typeHandle.m_Safety), null);
#else
                return new EnabledMask(SafeBitRef.Null, null);
#endif
            }

            var ptr = GetEnabledRefRWNoChange(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, typeHandle.m_LookupCache.IndexInArchetype,
                    out var ptrChunkDisabledCount)
                .Ptr;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var result = new EnabledMask(new SafeBitRef(ptr, 0, typeHandle.m_Safety), ptrChunkDisabledCount);
#else
            var result = new EnabledMask(new SafeBitRef(ptr, 0), ptrChunkDisabledCount);
#endif
            return result;
        }

        public static int ChunkIndex(this ArchetypeChunk chunk)
        {
            return UnsafeUtility.As<ChunkIndex, int>(ref chunk.m_Chunk);
        }

        private static UnsafeBitArray GetEnabledRefRWNoChange(ChunkIndex chunk, Archetype* archetype, int indexInTypeArray, out int* ptrChunkDisabledCount)
        {
            var chunkListIndex = chunk.ListIndex;
            var chunks = archetype->Chunks;

            var memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[indexInTypeArray];
            ptrChunkDisabledCount = chunks.GetPointerToChunkDisabledCountForType(memoryOrderIndexInArchetype, chunkListIndex);
            return chunks.GetEnabledArrayForTypeInChunk(memoryOrderIndexInArchetype, chunkListIndex);
        }

#if UNITY_INCLUDE_INSTRUMENTATION && !DISABLE_ENTITIES_JOURNALING
#pragma warning disable 0618
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void JournalAddRecord(
            this ref ArchetypeChunk chunk, EntitiesJournaling.RecordType recordType, TypeIndex typeIndex, uint globalSystemVersion, void* data = null,
            int dataLength = 0)
        {
            fixed (ArchetypeChunk* archetypeChunk = &chunk)
            {
                EntitiesJournaling.AddRecord(recordType, archetypeChunk->m_EntityComponentStore, globalSystemVersion, archetypeChunk, 1, types: &typeIndex,
                    typeCount: 1, data: data, dataLength: dataLength);
            }
        }
#pragma warning restore 0618
#endif
    }
}
