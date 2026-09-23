namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct UnsafeComponentLookup<T>
        where T : unmanaged, IComponentData
    {
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* _access;

        private readonly TypeIndex _typeIndex;
        private readonly byte _isZeroSized;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly byte _isReadOnly;
#endif

        private LookupCache _cache;
        private uint _globalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal UnsafeComponentLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            _isReadOnly = isReadOnly ? (byte)1 : (byte)0;
            _typeIndex = typeIndex;
            _access = access;
            _cache = default;
            _globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            _isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }
#else
        internal UnsafeComponentLookup(int typeIndex, EntityDataAccess* access)
        {
            _typeIndex = typeIndex;
            _access = access;
            _cache = default;
            _globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            _isZeroSized = ComponentType.FromTypeIndex(typeIndex).IsZeroSized ? (byte)1 : (byte)0;
        }
#endif

        public T this[Entity entity]
        {
            get
            {
                var ecs = _access->EntityComponentStore;
                ecs->AssertEntityHasComponent(entity, _typeIndex, ref _cache);

                if (_isZeroSized != 0)
                {
                    return default;
                }

                void* ptr = ecs->GetComponentDataWithTypeRO(entity, _typeIndex, ref _cache);
                UnsafeUtility.CopyPtrToStructure(ptr, out T data);

                return data;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                CheckWriteAndThrow(this);
#endif
                var ecs = _access->EntityComponentStore;
                ecs->AssertEntityHasComponent(entity, _typeIndex, ref _cache);

                if (_isZeroSized != 0)
                {
                    return;
                }

                void* ptr = ecs->GetComponentDataWithTypeRW(entity, _typeIndex, _globalSystemVersion, ref _cache);
                UnsafeUtility.CopyStructureToPtr(ref value, ptr);
            }
        }

        public T this[SystemHandle system]
        {
            get => this[system.m_Entity];
            set => this[system.m_Entity] = value;
        }

        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            _globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

        public bool HasComponent(Entity entity)
        {
            var ecs = _access->EntityComponentStore;
            return ecs->HasComponent(entity, _typeIndex, ref _cache, out _);
        }

        public bool HasComponent(SystemHandle system)
        {
            var ecs = _access->EntityComponentStore;
            return ecs->HasComponent(system.m_Entity, _typeIndex, ref _cache, out _);
        }

        public bool TryGetComponent(Entity entity, out T componentData)
        {
            var ecs = _access->EntityComponentStore;

            if (_isZeroSized != 0)
            {
                componentData = default;
                return ecs->HasComponent(entity, _typeIndex, ref _cache, out _);
            }

            if (Hint.Unlikely(!ecs->Exists(entity)))
            {
                componentData = default;
                return false;
            }

            void* ptr = ecs->GetOptionalComponentDataWithTypeRO(entity, _typeIndex, ref _cache);
            if (ptr == null)
            {
                componentData = default;
                return false;
            }

            UnsafeUtility.CopyPtrToStructure(ptr, out componentData);
            return true;
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

        public bool IsComponentEnabled(Entity entity)
        {
            return _access->IsComponentEnabled(entity, _typeIndex, ref _cache);
        }

        public bool IsComponentEnabled(SystemHandle systemHandle)
        {
            return _access->IsComponentEnabled(systemHandle.m_Entity, _typeIndex, ref _cache);
        }

        public void SetComponentEnabled(SystemHandle systemHandle, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckWriteAndThrow(this);
#endif
            _access->SetComponentEnabled(systemHandle.m_Entity, _typeIndex, value, ref _cache);
        }

        public void SetComponentEnabled(Entity entity, bool value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckWriteAndThrow(this);
#endif
            _access->SetComponentEnabled(entity, _typeIndex, value, ref _cache);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckWriteAndThrow(in UnsafeComponentLookup<T> componentLookup)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentLookup._isReadOnly != 0)
            {
                throw new InvalidOperationException("Writing when read only");
            }
#endif
        }
    }
}
