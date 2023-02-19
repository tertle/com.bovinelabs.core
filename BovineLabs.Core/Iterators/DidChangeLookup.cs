// <copyright file="DidChangeLookup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using Unity.Burst.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DidChangeLookup<T>
        where T : unmanaged
    {
        private readonly TypeIndex typeIndex;
        [NativeDisableUnsafePtrRestriction]
        private readonly EntityDataAccess* access;
        private LookupCache cache;

        internal DidChangeLookup(TypeIndex typeIndex, EntityDataAccess* access)
        {
            this.typeIndex = typeIndex;
            this.access = access;
            this.cache = default;
        }

        public bool DidChange(Entity entity, uint version)
        {
            var ecs = this.access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = chunk->Archetype;
            if (Hint.Unlikely(archetype != this.cache.Archetype))
            {
                this.cache.Update(archetype, this.typeIndex);
            }

            var typeIndexInArchetype = this.cache.IndexInArchetype;
            if (typeIndexInArchetype == -1)
            {
                return false;
            }

            var chunkVersion = chunk->GetChangeVersion(typeIndexInArchetype);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }
    }
}
