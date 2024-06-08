// <copyright file="WriteGroupMatcher.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    public struct WriteGroupMatcher<T> : IDisposable
    {
        [ReadOnly]
        private NativeArray<ComponentType> enableComponentTypes;
        private EntityQueryMask entityQueryMask;

        public WriteGroupMatcher(ref SystemState state)
        {
            TypeManagerUtil.GetWriteGroupComponents<T>(Allocator.Persistent, out this.enableComponentTypes, out var normalComponents);
            foreach (var c in this.enableComponentTypes)
            {
                state.AddDependency(c);
            }

            // Normal components don't need dependency added to system as they're only checked so structural changes will only be main thread
            if (normalComponents.Length > 0)
            {
                using var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
                foreach (var c in normalComponents)
                {
                    queryBuilder.WithAny(c);
                }

                using var query = queryBuilder.Build(state.EntityManager);
                this.entityQueryMask = query.GetEntityQueryMask();
            }
            else
            {
                this.entityQueryMask = default;
            }

            normalComponents.Dispose();
        }

        public void Dispose()
        {
            this.enableComponentTypes.Dispose();
        }

        public BitArray128 Matches(ArchetypeChunk archetypeChunk)
        {
            if (this.entityQueryMask.IsCreated() && this.entityQueryMask.MatchesIgnoreFilter(archetypeChunk))
            {
                return BitArray128.All;
            }

            var matches = BitArray128.None;
            foreach (var componentType in this.enableComponentTypes)
            {
                ref readonly var bits = ref UnsafeEntityDataAccess.GetRequiredEnabledBitsRO(archetypeChunk, componentType);
                matches |= new BitArray128(bits);
            }

            return matches;
        }
    }
}
