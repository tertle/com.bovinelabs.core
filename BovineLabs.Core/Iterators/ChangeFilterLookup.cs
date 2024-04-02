// <copyright file="ChangeFilterLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private readonly bool isReadOnly;
#endif

        private readonly TypeIndex typeIndex;

        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;
        private LookupCache cache;
        private uint globalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal ChangeFilterLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
            this.isReadOnly = isReadOnly;
        }
#else
        internal ChangeFilterLookup(TypeIndex typeIndex, EntityDataAccess* access)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.cache = default;
            this.globalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;
        }
#endif

        public void Update(SystemBase system)
        {
            this.Update(ref *system.m_StatePtr);
        }

        public void Update(ref SystemState systemState)
        {
            this.globalSystemVersion = systemState.m_EntityComponentStore->GlobalSystemVersion;
        }

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
            if (Hint.Unlikely(typeIndexInArchetype == -1))
            {
                return false;
            }

            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        public void SetChangeFilter(Entity entity)
        {
            var ecs = this.access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);

            this.SetChangeFilter(chunk, this.globalSystemVersion);
        }

        public void SetChangeFilter(int chunkIndex)
        {
            this.SetChangeFilter(new ChunkIndex(chunkIndex), this.globalSystemVersion);
        }

        private void SetChangeFilter(ChunkIndex chunk, uint systemVersion)
        {
            this.SetChangeFilterCheckWriteAndThrow();
            var ecs = this.access->EntityComponentStore;
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            var typeIndexInArchetype = this.cache.IndexInArchetype;
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
            if (this.isReadOnly)
            {
                throw new ArgumentException("SetChangeFilter used on read only");
            }
#endif
        }
    }
}
