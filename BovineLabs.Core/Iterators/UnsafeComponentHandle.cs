// <copyright file="UnsafeComponentHandle.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using BovineLabs.Core.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeComponentHandle
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        private uint globalSystemVersion;

        internal UnsafeComponentHandle(EntityDataAccess* access)
        {
            this.access = access;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
        }

        public void Update(ref SystemState systemState)
        {
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

        public readonly void* GetComponentDataPtrRW(ArchetypeChunk archetypeChunk, ComponentType componentType)
        {
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);

            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);

            var offset = archetype->Offsets[indexInTypeArray];

            archetype->Chunks.SetChangeVersion(indexInTypeArray, archetypeChunk.m_Chunk.ListIndex, this.globalSystemVersion);

            return archetypeChunk.m_Chunk.Buffer + offset;
        }

        public readonly void* GetComponentDataPtrRO(Entity entity, ComponentType componentType)
        {
            var ecs = this.access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, componentType.TypeIndex);
            return ecs->GetComponentDataWithTypeRO(entity, componentType.TypeIndex);
        }

        public readonly void* GetChunkComponentDataPtrRW(ArchetypeChunk archetypeChunk, ComponentType componentType)
        {
            access->EntityComponentStore->AssertEntityHasComponent(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
            return access->EntityComponentStore->GetComponentDataWithTypeRW(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex, globalSystemVersion);
        }

        public readonly void* GetChunkComponentDataPtrRO(ArchetypeChunk archetypeChunk, ComponentType componentType)
        {
            access->EntityComponentStore->AssertEntityHasComponent(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
            return access->EntityComponentStore->GetComponentDataWithTypeRO(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
        }

        public readonly UnsafeUntypedDynamicBufferAccessor GetDynamicBufferAccessor(ArchetypeChunk chunk, ComponentType componentType)
        {
            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            // var archetype = chunk.m_Chunk->Archetype;
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);

            var internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];
            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
            var ptr = ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, archetype, 0, typeIndexInArchetype, this.globalSystemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var elementSize = typeInfo.ElementSize;

            return new UnsafeUntypedDynamicBufferAccessor(ptr, length, stride, elementSize, internalCapacity);
        }

        public readonly UnsafeUntypedDynamicBuffer GetUntypedBuffer(Entity entity, ComponentType componentType)
        {
            var typeIndex = componentType.TypeIndex;

            var header = (BufferHeader*)access->EntityComponentStore->GetComponentDataWithTypeRW(
                entity, typeIndex, access->EntityComponentStore->GlobalSystemVersion);

            var internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            var typeInfo = TypeManager.GetTypeInfo(typeIndex);

            return new UnsafeUntypedDynamicBuffer(header, internalCapacity, typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
        }
    }
}
