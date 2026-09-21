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
        private NativeArray<ComponentType> _enableComponentTypes;
        private EntityQueryMask _entityQueryMask;

        public WriteGroupMatcher(ref SystemState state)
        {
            TypeManagerUtil.GetWriteGroupComponents<T>(Allocator.Persistent, out _enableComponentTypes, out var normalComponents);
            foreach (var c in _enableComponentTypes)
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

        public BitArray128 Matches(ArchetypeChunk archetypeChunk)
        {
            if (_entityQueryMask.IsCreated() && _entityQueryMask.MatchesIgnoreFilter(archetypeChunk))
            {
                return BitArray128.All;
            }

            var matches = BitArray128.None;
            foreach (var componentType in _enableComponentTypes)
            {
                ref readonly var bits = ref UnsafeEntityDataAccess.GetRequiredEnabledBitsRO(archetypeChunk, componentType);
                matches |= new BitArray128(bits);
            }

            return matches;
        }
    }
}
