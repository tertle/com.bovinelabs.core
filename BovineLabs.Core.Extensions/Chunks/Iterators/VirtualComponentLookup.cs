// <copyright file="VirtualComponentLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [NativeContainer]
    public unsafe struct VirtualComponentLookup<T>
        where T : unmanaged, IComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        private readonly TypeIndex chunkLinksTypeIndex;
        private readonly byte groupIndex;

        private LookupCache cache;
        private LookupCache chunkLinksLookupCache;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
#endif
        private readonly TypeIndex typeIndex;
        private uint globalSystemVersion;
        private readonly byte isZeroSized; // cache of whether T is zero-sized
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly byte isReadOnly;
#endif

        internal uint GlobalSystemVersion => this.globalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal VirtualComponentLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            var safetyHandles = &access->DependencyManager->Safety;
            this.m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(typeIndex, isReadOnly);
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.typeIndex = typeIndex;
            this.access = access;
            this.chunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.groupIndex = TypeMangerEx.GetGroupIndex<T>();
            this.cache = default;
            this.chunkLinksLookupCache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }

#else
        internal VirtualComponentLookup(int typeIndex, EntityDataAccess* access)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.chunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.groupIndex = TypeMangerEx.GetGroupIndex<T>();
            this.cache = default;
            this.chunkLinksLookupCache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }
#endif

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system">The system on which this type handle is cached.</param>
        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a ComponentLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="systemState">The SystemState of the system on which this type handle is cached.</param>
        public void Update(ref SystemState systemState)
        {
            // NOTE: We could in theory fetch all this data from m_Access.EntityComponentStore and void the SystemState from being passed in.
            //       That would unfortunately allow this API to be called from a job. So we use the required system parameter as a way of signifying to the user that this can only be invoked from main thread system code.
            //       Additionally this makes the API symmetric to ComponentTypeHandle.
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &this.access->DependencyManager->Safety;
            m_Safety = safetyHandles->GetSafetyHandleForComponentLookup(this.typeIndex, this.isReadOnly != 0);
#endif
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        public bool HasComponent(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            var ecs = this.access->EntityComponentStore;

            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                return false;
            }

            this.GetChunkAndIndex(entity, out var chunk, out _);

            var archetype = ecs->GetArchetype(chunk);
            return this.HasComponent(archetype);
        }

        /// <summary>
        /// Gets a safe reference to the component data and a default RefRW (RefRW.IsValid == false).
        /// /// </summary>
        /// <param name="entity">The referenced entity</param>
        /// <param name="isReadOnly">True if you only want to read from the returned component; false if you also want to write to it</param>
        /// <returns>Returns a safe reference to the component data and a default RefRW.</returns>
        public RefRW<T> GetRefRW(Entity entity, bool isReadOnly)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            if (this.isZeroSized != 0)
            {
                return default;
            }

            this.GetChunkAndIndex(entity, out var chunk, out var indexInChunk);

            var ecs = this.access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);

            if (!this.HasComponent(archetype))
            {
                return default;
            }

            void* ptr = isReadOnly
                    ? this.access->GetComponentDataWithTypeRO(chunk, archetype, indexInChunk, this.typeIndex, ref this.cache)
                    : this.access->GetComponentDataWithTypeRW(chunk, archetype, indexInChunk, entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRW<T>(ptr, this.m_Safety);
#else
            return new RefRW<T> (ptr);
#endif
        }

        /// <summary>
        /// Gets a safe reference to the component data and
        /// a default RefRO (RefRO.IsValid == false).
        /// </summary>
        /// <param name="entity">The referenced entity</param>
        /// <returns>Returns a safe reference to the component data and a default RefRW.</returns>
        public RefRO<T> GetRefRO(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            if (this.isZeroSized != 0)
            {
                return default;
            }

            this.GetChunkAndIndex(entity, out var chunk, out var indexInChunk);

            var ecs = this.access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);

            if (!this.HasComponent(archetype))
            {
                return default;
            }

            void* ptr = this.access->GetComponentDataWithTypeRO(chunk, archetype, indexInChunk, this.typeIndex, ref this.cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new RefRO<T>(ptr, this.m_Safety);
#else
            return new RefRO<T> (ptr);
#endif
        }

        /// <summary>
        /// Gets a safe reference to the component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <param name="isReadOnly">True if you only want to read from the returned component enabled state; false if you also want to write to it</param>
        /// <returns>Returns a safe reference to the component enabled state. If the component
        /// doesn't exist, it returns a default ComponentEnabledRefRW.</returns>
        public EnabledRefRW<T2> GetComponentEnabledRefRW<T2>(Entity entity, bool isReadOnly)
            where T2 : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            this.GetChunkAndIndex(entity, out var chunk, out var indexInBitField);

            var ecs = this.access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);

            if (!this.HasComponent(archetype))
            {
                return new EnabledRefRW<T2>(default, default);
            }

            var ptr = isReadOnly
                    ? GetEnabledRawRO(ecs, chunk, this.typeIndex, ref this.cache, out var ptrChunkDisabledCount)
                    : GetEnabledRawRW(ecs, chunk, this.typeIndex, ref this.cache, this.globalSystemVersion, out ptrChunkDisabledCount);

            return new EnabledRefRW<T2>(this.MakeSafeBitRef(ptr, indexInBitField), ptrChunkDisabledCount);
        }

        /// <summary>
        /// Gets a safe reference to the component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type.</typeparam>
        /// <param name="entity">The referenced entity.</param>
        /// <returns> Returns a safe reference to the component enabled state.
        /// If the component doesn't exist, returns a default ComponentEnabledRefRO.</returns>
        public EnabledRefRO<T2> GetComponentEnabledRefRO<T2>(Entity entity)
            where T2 : unmanaged, IComponentData, IEnableableComponent
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
            this.GetChunkAndIndex(entity, out var chunk, out var indexInBitField);

            var ecs = this.access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);

            if (!this.HasComponent(archetype))
            {
                return new EnabledRefRO<T2>(default);
            }

            var ptr = GetEnabledRawRO(ecs, chunk, this.typeIndex, ref this.cache, out _);
            return new EnabledRefRO<T2>(this.MakeSafeBitRef(ptr, indexInBitField));
        }

        private SafeBitRef MakeSafeBitRef(ulong* ptr, int offsetInBits)
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            => new SafeBitRef(ptr, offsetInBits, this.m_Safety);
#else
            => new SafeBitRef(ptr, offsetInBits);
#endif

        private static ulong* GetEnabledRawRO(EntityComponentStore* ecs, ChunkIndex chunk, TypeIndex typeIndex, ref LookupCache typeLookupCache, out int* ptrChunkDisabledCount)
        {
            // TODO ASSERT HAS
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
            {
                typeLookupCache.Update(archetype, typeIndex);
            }

            var memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeLookupCache.IndexInArchetype];
            ptrChunkDisabledCount = archetype->Chunks.GetPointerToChunkDisabledCountForType(memoryOrderIndexInArchetype, chunk.ListIndex);
            return ChunkDataUtility.GetEnabledRefRO(chunk, archetype, typeLookupCache.IndexInArchetype).Ptr;
        }

        private static ulong* GetEnabledRawRW(EntityComponentStore* ecs,  ChunkIndex chunk, TypeIndex typeIndex, ref LookupCache typeLookupCache, uint globalSystemVersion, out int* ptrChunkDisabledCount)
        {
            // TODO ASSERT HAS
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
            {
                typeLookupCache.Update(archetype, typeIndex);
            }

            return ChunkDataUtility.GetEnabledRefRW(chunk, archetype, typeLookupCache.IndexInArchetype, globalSystemVersion, out ptrChunkDisabledCount).Ptr;
        }

        private void GetChunkAndIndex(Entity entity, out ChunkIndex chunk, out int index)
        {
            var ecs = this.access->EntityComponentStore;
            var entityInChunk = ecs->GetEntityInChunk(entity);
            chunk = VirtualChunkDataUtility.GetChunk(ecs, entityInChunk.Chunk, this.groupIndex, this.chunkLinksTypeIndex, ref this.chunkLinksLookupCache);

            // The index in the virtual chunk should match the index of the original entity in it's chunk
            index = entityInChunk.IndexInChunk;
        }

        private bool HasComponent(Archetype* archetype)
        {
            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            return this.cache.IndexInArchetype != -1;
        }
    }
}
#endif
