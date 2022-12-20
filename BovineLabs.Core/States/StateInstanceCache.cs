// <copyright file="StateInstanceCache.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Entities;

    public static class StateInstanceCache
    {
        public static NativeArray<StateSystemTypes> GetAllStateSystems(ref SystemState state)
        {
            if (!state.EntityManager.TryGetSingletonBuffer<StateSystemTypes>(out var buffer))
            {
                var entity = state.EntityManager.CreateEntity();
                buffer = state.EntityManager.AddBuffer<StateSystemTypes>(entity);

                using var query = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll(WorldUnmanagedInternal.SystemInstanceComponentType)
                    .WithOptions(EntityQueryOptions.IncludeSystems)
                    .Build(state.EntityManager);

                using var entities = query.ToEntityArray(Allocator.Temp);

                foreach (var systemEntity in entities)
                {
                    if (!state.EntityManager.HasComponent<StateInstance>(systemEntity))
                    {
                        continue;
                    }

                    var component = state.EntityManager.GetComponentData<StateInstance>(systemEntity);
                    buffer.Add(new StateSystemTypes { Value = component });
                }
            }

            return buffer.AsNativeArray();
        }

        public struct StateSystemTypes : IBufferElementData
        {
            public StateInstance Value;
        }
    }
}
