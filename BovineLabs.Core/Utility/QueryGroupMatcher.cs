namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>Matches direct query-group members in a chunk, respecting enableable component and buffer state.</summary>
    public unsafe struct QueryGroupMatcher<T> : IDisposable
    {
        [ReadOnly]
        private NativeArray<ComponentType> _enableComponentTypes;
        private EntityQueryMask _entityQueryMask;

        public QueryGroupMatcher(ref SystemState state)
        {
            TypeManagerUtil.GetQueryGroupComponents<T>(Allocator.Persistent, out _enableComponentTypes, out var normalComponents);
            foreach (var component in _enableComponentTypes)
            {
                state.AddDependency(component);
            }

            if (normalComponents.Length > 0)
            {
                using var builder = new EntityQueryBuilder(Allocator.Temp)
                    .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab);
                foreach (var component in normalComponents)
                {
                    builder.WithAny(component);
                }

                using var query = builder.Build(state.EntityManager);
                _entityQueryMask = query.GetEntityQueryMask();
            }
            else
            {
                _entityQueryMask = default;
            }

            normalComponents.Dispose();
        }

        public void Dispose()
        {
            _enableComponentTypes.Dispose();
        }

        public BitArray128 Matches(ArchetypeChunk chunk)
        {
            if (_entityQueryMask.IsCreated() && _entityQueryMask.MatchesIgnoreFilter(chunk))
            {
                return BitArray128.All;
            }

            var matches = BitArray128.None;
            foreach (var component in _enableComponentTypes)
            {
                // Group members are optional in any particular archetype.
                if (ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype.Archetype, component.TypeIndex) < 0)
                {
                    continue;
                }

                ref readonly var bits = ref UnsafeEntityDataAccess.GetRequiredEnabledBitsRO(chunk, component);
                matches |= new BitArray128(bits);
            }

            return matches;
        }
    }
}
