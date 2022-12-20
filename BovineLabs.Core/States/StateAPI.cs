// <copyright file="StateAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using Unity.Collections;
    using Unity.Entities;

    public static class StateAPI
    {
        public static void Register<TState, TInstance>(ref SystemState state, byte stateKey, bool queryDependency = true)
            where TState : unmanaged, IComponentData
            where TInstance : unmanaged, IComponentData
        {
            if (queryDependency)
            {
                var query = new EntityQueryBuilder(Allocator.Temp).WithAll<TInstance>().Build(ref state);
                state.RequireForUpdate(query);
            }

            state.EntityManager.AddComponentData(state.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<TState>(),
                StateKey = stateKey,
                StateInstanceComponent = ComponentType.ReadOnly<TInstance>().TypeIndex,
            });
        }
    }
}
