namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct ChangeFilterLookup<T>
        where T : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly bool _isReadOnly;
#endif

        private readonly TypeIndex _typeIndex;

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* _access;
        private LookupCache _cache;
        private uint _globalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal ChangeFilterLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            _typeIndex = typeIndex;
            _access = access;
            _cache = default;
            _globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            _isReadOnly = isReadOnly;
        }
#else
        internal ChangeFilterLookup(TypeIndex typeIndex, EntityDataAccess* access)
        {
            _typeIndex = typeIndex;
            _access = access;
            _cache = default;
            _globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
        }
#endif

        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            _globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

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
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return false;
            }

            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        public void SetChangeFilter(Entity entity)
        {
            var ecs = _access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);

            SetChangeFilter(chunk, _globalSystemVersion);
        }

        public void SetChangeFilter(int chunkIndex)
        {
            SetChangeFilter(new ChunkIndex(chunkIndex), _globalSystemVersion);
        }

        private void SetChangeFilter(ChunkIndex chunk, uint systemVersion)
        {
            SetChangeFilterCheckWriteAndThrow();
            var ecs = _access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != _cache.Archetype))
            {
                _cache.Update(archetype, _typeIndex);
            }

            var typeIndexInArchetype = _cache.IndexInArchetype;
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return;
            }

            archetype->Chunks.SetChangeVersion(typeIndexInArchetype, chunk.ListIndex, systemVersion);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void SetChangeFilterCheckWriteAndThrow()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (_isReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only");
            }
#endif
        }
    }
}
