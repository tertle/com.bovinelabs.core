// <copyright file="StateInstanceUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using Unity.Collections;
    using Unity.Entities;

    public static class StateInstanceUtil
    {
        public static NativeArray<StateInstance> GetAllStateInstances(ref SystemState state, Allocator allocator = Allocator.Temp)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StateInstance>()
                .WithOptions(EntityQueryOptions.IncludeSystems)
                .Build(state.EntityManager);

            return query.ToComponentDataArray<StateInstance>(allocator);
        }
    }
}
