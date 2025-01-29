// <copyright file="UnsafeEntityDataAccess.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeEntityDataAccess
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        internal UnsafeEntityDataAccess(EntityDataAccess* access)
        {
            this.access = access;
            this.GlobalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
        }

        public uint GlobalSystemVersion { get; private set; }

        public void Update(ref SystemState systemState)
        {
            this.GlobalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

        public static byte* GetComponentDataPtrRW(ArchetypeChunk archetypeChunk, ComponentType componentType, uint globalSystemVersion)
        {
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);
            var offset = archetype->Offsets[indexInTypeArray];
            archetype->Chunks.SetChangeVersion(indexInTypeArray, archetypeChunk.m_Chunk.ListIndex, globalSystemVersion);
            return archetypeChunk.m_Chunk.Buffer + offset;
        }

        public static byte* GetChunkComponentDataPtrRW(ArchetypeChunk archetypeChunk, ComponentType componentType, uint globalSystemVersion)
        {
            archetypeChunk.m_EntityComponentStore->AssertEntityHasComponent(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
            return archetypeChunk.m_EntityComponentStore->GetComponentDataWithTypeRW(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex,
                globalSystemVersion);
        }

        public static byte* GetChunkComponentDataPtrRO(ArchetypeChunk archetypeChunk, ComponentType componentType)
        {
            archetypeChunk.m_EntityComponentStore->AssertEntityHasComponent(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
            return archetypeChunk.m_EntityComponentStore->GetComponentDataWithTypeRO(archetypeChunk.m_Chunk.MetaChunkEntity, componentType.TypeIndex);
        }

        public static UnsafeUntypedDynamicBufferAccessor GetDynamicBufferAccessor(ArchetypeChunk chunk, ComponentType componentType, uint globalSystemVersion)
        {
            var archetype = chunk.m_EntityComponentStore->GetArchetype(chunk.m_Chunk);

            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);

            var internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];
            var typeInfo = TypeManager.GetTypeInfo(componentType.TypeIndex);
            var ptr = ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, archetype, 0, typeIndexInArchetype, globalSystemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];
            var elementSize = typeInfo.ElementSize;

            return new UnsafeUntypedDynamicBufferAccessor(ptr, length, stride, elementSize, internalCapacity);
        }

        public static ref readonly v128 GetRequiredEnabledBitsRO(ArchetypeChunk archetypeChunk, ComponentType componentType)
        {
            var archetype = archetypeChunk.m_EntityComponentStore->GetArchetype(archetypeChunk.m_Chunk);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            // Must check this after computing the pointer, to make sure the cache is up to date
            if (Hint.Unlikely(indexInTypeArray == -1))
            {
                throw new ArgumentException($"Required component {componentType.ToFixedString()} not found in archetype.");
            }
#endif

            var ptr = ChunkDataUtility.GetEnabledRefRO(archetypeChunk.m_Chunk, archetypeChunk.Archetype.Archetype, indexInTypeArray).Ptr;
            return ref UnsafeUtility.AsRef<v128>(ptr);
        }

        public readonly bool HasComponent(Entity entity, ComponentType componentType)
        {
            return this.access->EntityComponentStore->HasComponent(entity, componentType, out _);
        }

        public readonly bool HasComponent(Entity entity, TypeIndex typeIndex)
        {
            return this.access->EntityComponentStore->HasComponent(entity, typeIndex, out _);
        }

        /// <summary> Reports whether the specified <see cref="Entity" /> instance still refers to a valid entity. </summary>
        /// <param name="entity"> The entity. </param>
        /// <returns>
        /// True if the entity exists and is valid, and returns false if
        /// the Entity instance refers to an entity that has been destroyed.
        /// </returns>
        public readonly bool Exists(Entity entity)
        {
            return this.access->EntityComponentStore->Exists(entity);
        }

        /// <summary> Gets an <see cref="EntityStorageInfo" /> for the specified entity. </summary>
        /// <param name="entity"> The entity. </param>
        /// <exception cref="System.ArgumentException"> Thrown if T is zero-size. </exception>
        public readonly EntityStorageInfo GetEntityStorageInfo(Entity entity)
        {
            this.access->EntityComponentStore->AssertEntitiesExist(&entity, 1);

            var entityInChunk = this.access->EntityComponentStore->GetEntityInChunk(entity);

            return new EntityStorageInfo
            {
                Chunk = new ArchetypeChunk(entityInChunk.Chunk, this.access->EntityComponentStore),
                IndexInChunk = entityInChunk.IndexInChunk,
            };
        }

        public readonly byte* GetRequiredComponentDataPtrRO(Entity entity, ComponentType componentType)
        {
            return this.GetRequiredComponentDataPtrRO(entity, componentType.TypeIndex);
        }

        public readonly byte* GetRequiredComponentDataPtrRO(Entity entity, TypeIndex typeIndex)
        {
            var ecs = this.access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, typeIndex);
            return ecs->GetComponentDataWithTypeRO(entity, typeIndex);
        }

        public readonly byte* GetRequiredComponentDataPtrRW(Entity entity, ComponentType componentType)
        {
            return this.GetRequiredComponentDataPtrRW(entity, componentType.TypeIndex);
        }

        public readonly byte* GetRequiredComponentDataPtrRW(Entity entity, TypeIndex typeIndex)
        {
            var ecs = this.access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, typeIndex);
            return ecs->GetComponentDataWithTypeRW(entity, typeIndex, this.GlobalSystemVersion);
        }

        public readonly byte* GetComponentDataPtrRO(Entity entity, ComponentType componentType)
        {
            return this.HasComponent(entity, componentType)
                ? this.access->EntityComponentStore->GetComponentDataWithTypeRO(entity, componentType.TypeIndex)
                : null;
        }

        public readonly byte* GetComponentDataPtrRW(Entity entity, ComponentType componentType)
        {
            return this.HasComponent(entity, componentType)
                ? this.access->EntityComponentStore->GetComponentDataWithTypeRW(entity, componentType.TypeIndex, this.GlobalSystemVersion)
                : null;
        }

        public readonly UnsafeUntypedDynamicBuffer GetUntypedBufferRO(Entity entity, ComponentType componentType)
        {
            var typeIndex = componentType.TypeIndex;

            var header = (BufferHeader*)this.access->EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

            var internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            var typeInfo = TypeManager.GetTypeInfo(typeIndex);

            return new UnsafeUntypedDynamicBuffer(header, internalCapacity, typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
        }

        public readonly UnsafeUntypedDynamicBuffer GetUntypedBufferRW(Entity entity, ComponentType componentType)
        {
            var typeIndex = componentType.TypeIndex;

            var header = (BufferHeader*)this.access->EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex, this.GlobalSystemVersion);

            var internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;
            var typeInfo = TypeManager.GetTypeInfo(typeIndex);

            return new UnsafeUntypedDynamicBuffer(header, internalCapacity, typeInfo.ElementSize, UntypedDynamicBuffer.AlignOf);
        }
    }
}
