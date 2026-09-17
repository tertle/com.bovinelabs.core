namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeBufferLookup<T>
        where T : unmanaged, IBufferElementData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;

        private readonly TypeIndex typeIndex;
        private readonly byte isReadOnly;

        private LookupCache cache;
        private uint globalSystemVersion;
        private int internalCapacity;

        internal UnsafeBufferLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.internalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;
        }

        /// <summary>
        /// Parallel writes require proving that threads cannot access the same buffer before disabling the parallel restriction.
        /// </summary>
        public UnsafeDynamicBuffer<T> this[Entity entity]
        {
            get
            {
                var ecs = this.access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                ecs->AssertEntityHasComponent(entity, this.typeIndex, ref this.cache);
#endif

                var header = this.isReadOnly != 0
                    ? (BufferHeader*)ecs->GetComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache)
                    : (BufferHeader*)ecs->GetComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

                return new UnsafeDynamicBuffer<T>(header, this.internalCapacity);
            }
        }

        public bool TryGetBuffer(Entity entity, out UnsafeDynamicBuffer<T> bufferData)
        {
            var ecs = this.access->EntityComponentStore;
            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                bufferData = default;
                return false;
            }

            var header = this.isReadOnly != 0
                ? (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRO(entity, this.typeIndex, ref this.cache)
                : (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRW(entity, this.typeIndex, this.globalSystemVersion, ref this.cache);

            if (header != null)
            {
                bufferData = new UnsafeDynamicBuffer<T>(header, this.internalCapacity);
                return true;
            }

            bufferData = default;
            return false;
        }

        public bool HasBuffer(Entity entity)
        {
            var ecs = this.access->EntityComponentStore;
            return ecs->HasComponent(entity, this.typeIndex, ref this.cache, out _);
        }

        /// <summary>
        /// Tracks possible writes to the whole chunk, including declared writes that leave values unchanged.
        /// </summary>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = this.access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            var typeIndexInArchetype = this.cache.IndexInArchetype;
            if (typeIndexInArchetype == -1)
            {
                return false;
            }

            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        public bool IsBufferEnabled(Entity entity)
        {
            return this.access->IsComponentEnabled(entity, this.typeIndex, ref this.cache);
        }

        public void SetBufferEnabled(Entity entity, bool value)
        {
            this.access->SetComponentEnabled(entity, this.typeIndex, value, ref this.cache);
        }

        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            // NOTE: We could in theory fetch all this data from m_Access.EntityComponentStore and void the SystemState from being passed in.
            //       That would unfortunately allow this API to be called from a job. So we use the required system parameter as a way of signifying to the user that this can only be invoked from main thread system code.
            //       Additionally this makes the API symmetric to ComponentTypeHandle.
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }
    }
}
