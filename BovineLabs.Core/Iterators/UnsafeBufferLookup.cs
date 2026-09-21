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
        private readonly EntityDataAccess* _access;

        private readonly TypeIndex _typeIndex;
        private readonly byte _isReadOnly;

        private LookupCache _cache;
        private uint _globalSystemVersion;
        private int _internalCapacity;

        internal UnsafeBufferLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            _typeIndex = typeIndex;
            _access = access;
            _isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            _cache = default;
            _globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            _internalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;
        }

        /// <summary>
        /// Parallel writes require proving that threads cannot access the same buffer before disabling the parallel restriction.
        /// </summary>
        public UnsafeDynamicBuffer<T> this[Entity entity]
        {
            get
            {
                var ecs = _access->EntityComponentStore;
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                ecs->AssertEntityHasComponent(entity, _typeIndex, ref _cache);
#endif

                var header = _isReadOnly != 0
                    ? (BufferHeader*)ecs->GetComponentDataWithTypeRO(entity, _typeIndex, ref _cache)
                    : (BufferHeader*)ecs->GetComponentDataWithTypeRW(entity, _typeIndex, _globalSystemVersion, ref _cache);

                return new UnsafeDynamicBuffer<T>(header, _internalCapacity);
            }
        }

        public bool TryGetBuffer(Entity entity, out UnsafeDynamicBuffer<T> bufferData)
        {
            var ecs = _access->EntityComponentStore;
            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                bufferData = default;
                return false;
            }

            var header = _isReadOnly != 0
                ? (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRO(entity, _typeIndex, ref _cache)
                : (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRW(entity, _typeIndex, _globalSystemVersion, ref _cache);

            if (header != null)
            {
                bufferData = new UnsafeDynamicBuffer<T>(header, _internalCapacity);
                return true;
            }

            bufferData = default;
            return false;
        }

        public bool HasBuffer(Entity entity)
        {
            var ecs = _access->EntityComponentStore;
            return ecs->HasComponent(entity, _typeIndex, ref _cache, out _);
        }

        /// <summary>
        /// Tracks possible writes to the whole chunk, including declared writes that leave values unchanged.
        /// </summary>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = _access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != _cache.Archetype))
            {
                _cache.Update(archetype, _typeIndex);
            }

            var typeIndexInArchetype = _cache.IndexInArchetype;
            if (typeIndexInArchetype == -1)
            {
                return false;
            }

            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        public bool IsBufferEnabled(Entity entity)
        {
            return _access->IsComponentEnabled(entity, _typeIndex, ref _cache);
        }

        public void SetBufferEnabled(Entity entity, bool value)
        {
            _access->SetComponentEnabled(entity, _typeIndex, value, ref _cache);
        }

        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            // NOTE: We could in theory fetch all this data from m_Access.EntityComponentStore and void the SystemState from being passed in.
            //       That would unfortunately allow this API to be called from a job. So we use the required system parameter as a way of signifying to the user that this can only be invoked from main thread system code.
            //       Additionally this makes the API symmetric to ComponentTypeHandle.
            _globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }
    }
}
