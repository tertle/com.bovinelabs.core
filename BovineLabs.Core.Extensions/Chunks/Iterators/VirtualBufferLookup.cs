// <copyright file="VirtualBufferLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks.Iterators
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Extensions;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VirtualBufferLookup<T>
        where T : unmanaged, IBufferElementData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;
        private LookupCache cache;
        private LookupCache chunkLinksLookupCache;
        private readonly TypeIndex typeIndex;

        private readonly TypeIndex chunkLinksTypeIndex;
        private readonly byte groupIndex;

        private uint globalSystemVersion;
        private int internalCapacity;
        private readonly byte isReadOnly;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;
#endif

        internal uint GlobalSystemVersion => this.globalSystemVersion;


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal VirtualBufferLookup(
            TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly, AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.chunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.groupIndex = TypeMangerEx.GetGroupIndex<T>();
            this.cache = default;
            this.chunkLinksLookupCache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;

            if (!TypeManager.IsBuffer(this.typeIndex))
            {
                var typeName = this.typeIndex.ToFixedString();
                throw new ArgumentException(
                    $"GetComponentBufferArray<{typeName}> must be IBufferElementData");
            }

            this.internalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;

            this.m_Safety0 = safety;
            this.m_ArrayInvalidationSafety = arrayInvalidationSafety;
            this.m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            this.m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
        }

#else
        internal VirtualBufferLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;;
            this.chunkLinksTypeIndex = TypeManager.GetTypeIndex<ChunkLinks>();
            this.groupIndex = TypeMangerEx.GetGroupIndex<T>();
            this.cache = default;
            this.chunkLinksLookupCache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.internalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;
        }

#endif

        /// <summary>
        /// Retrieves the buffer components associated with the specified <see cref="Entity"/>, if it exists. Then reports if the instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// /// <param name="bufferData">The buffer component of type T for the given entity, if it exists.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not.</returns>
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> bufferData)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
#endif
            var ecs = this.access->EntityComponentStore;
            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                bufferData = default;
                return false;
            }

            var header = (this.isReadOnly != 0)
                ? (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache)
                : (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

            if (header != null)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                bufferData = new DynamicBuffer<T>(header, this.m_Safety0, this.m_ArrayInvalidationSafety, this.isReadOnly != 0, false, 0, this.internalCapacity);
#else
                bufferData = new DynamicBuffer<T>(header, this.internalCapacity);
#endif
                return true;
            }
            else
            {
                bufferData = default;
                return false;
            }
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        public bool HasBuffer(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
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
        /// Gets the <see cref="DynamicBuffer{T}"/> instance of type <typeparamref name="T"/> for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A <see cref="DynamicBuffer{T}"/> type.</returns>
        /// <remarks>
        /// Normally, you cannot write to buffers accessed using a BufferLookup instance
        /// in a parallel Job. This restriction is in place because multiple threads could write to the same buffer,
        /// leading to a race condition and nondeterministic results. However, when you are certain that your algorithm
        /// cannot write to the same buffer from different threads, you can manually disable this safety check
        /// by putting the [NativeDisableParallelForRestrictions] attribute on the BufferLookup field in the Job.
        ///
        /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
        /// </remarks>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="entity"/> does not have a buffer
        /// component of type <typeparamref name="T"/>.</exception>
        public DynamicBuffer<T> this[Entity entity]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Note that this check is only for the lookup table into the entity manager
                // The native array performs the actual read only / write only checks
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
#endif

                this.GetChunkAndIndex(entity, out var chunk, out var indexInChunk);

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (chunk == null)
                {
                    throw new ArgumentException($"A component with type:{this.typeIndex} has not been added to the entity." +
                                                EntityComponentStore.AppendRemovedComponentRecordError(entity, ComponentType.FromTypeIndex(this.typeIndex)));
                }
#endif

                var ecs = this.access->EntityComponentStore;
                var archetype = ecs->GetArchetype(chunk);

                var header = (this.isReadOnly != 0) ?
                    (BufferHeader*)this.access->GetComponentDataWithTypeRO(chunk, archetype, indexInChunk, this.typeIndex, ref this.cache) :
                    (BufferHeader*)this.access->GetComponentDataWithTypeRW(chunk, archetype,indexInChunk, entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return new DynamicBuffer<T>(header, this.m_Safety0, this.m_ArrayInvalidationSafety, this.isReadOnly != 0, false, 0, this.internalCapacity);
#else
                return new DynamicBuffer<T>(header, this.internalCapacity);
#endif
            }
        }

        /// <summary>
        /// Checks whether the <see cref="IBufferElementData"/> of type T is enabled on the specified <see cref="Entity"/>.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be checked.</param>
        /// <returns>True if the specified component is enabled, or false if it is disabled.</returns>
        /// <seealso cref="SetBufferEnabled"/>
        public bool IsBufferEnabled(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety0);
#endif
            this.GetChunkAndIndex(entity, out var chunk, out var indexInChunk);

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (chunk == null)
            {
                throw new ArgumentException($"A component with type:{this.typeIndex} has not been added to the entity." +
                                            EntityComponentStore.AppendRemovedComponentRecordError(entity, ComponentType.FromTypeIndex(this.typeIndex)));
            }
#endif

            var archetype = this.access->EntityComponentStore->GetArchetype(chunk);

            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            return this.access->EntityComponentStore->IsComponentEnabled(chunk, indexInChunk, this.cache.IndexInArchetype);
        }

        /// <summary>
        /// Enable or disable the <see cref="IBufferElementData"/> of type T on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// <seealso cref="IsBufferEnabled"/>
        public void SetBufferEnabled(Entity entity, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety0);
#endif
            this.GetChunkAndIndex(entity, out var chunk, out var indexInChunk);

#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (chunk == null)
            {
                throw new ArgumentException($"A component with type:{this.typeIndex} has not been added to the entity." +
                                            EntityComponentStore.AppendRemovedComponentRecordError(entity, ComponentType.FromTypeIndex(this.typeIndex)));
            }
#endif

            var archetype = this.access->EntityComponentStore->GetArchetype(chunk);

            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            this.access->EntityComponentStore->SetComponentEnabled(chunk, indexInChunk, this.cache.IndexInArchetype, value);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            if (Hint.Unlikely(this.access->EntityComponentStore->m_RecordToJournal != 0))
            {
                this.access->SetComponentEnabled(default, &entity, 1, this.typeIndex,value);
            }
#endif
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system">The system on which this type handle is cached.</param>
        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
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
            m_Safety0 = safetyHandles->GetSafetyHandleForComponentLookup(this.typeIndex, this.isReadOnly != 0);
            m_ArrayInvalidationSafety = safetyHandles->GetBufferHandleForBufferLookup(this.typeIndex);
#endif
        }

        private void GetChunkAndIndex(Entity entity, out ChunkIndex chunk, out int index)
        {
            var ecs = this.access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                throw new ArgumentException("The entity does not exist." + EntityComponentStore.AppendDestroyedEntityRecordError(entity));
            }
#endif

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
