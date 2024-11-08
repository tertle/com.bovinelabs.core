// <copyright file="StateAPI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Keys;
    using Unity.Collections;
    using Unity.Entities;

    public static class StateAPI
    {
        public static byte Register<TState, TInstance, TSettings>(ref SystemState systemState, string stateName, bool queryDependency = true)
            where TState : unmanaged, IComponentData
            where TInstance : unmanaged, IComponentData
        {
            var stateKey = (byte)K<TSettings>.NameToKey(stateName);
            Register<TState, TInstance>(ref systemState, stateKey, queryDependency);
            return stateKey;
        }

        public static void Register<TState, TInstance>(ref SystemState systemState, byte stateKey, bool queryDependency = true)
            where TState : unmanaged, IComponentData
            where TInstance : unmanaged, IComponentData
        {
            Check.Assume(!systemState.EntityManager.HasComponent<StateInstance>(systemState.SystemHandle), "Trying to register more than 1 state to a system.");

            if (queryDependency)
            {
                var query = new EntityQueryBuilder(Allocator.Temp).WithAll<TInstance>().Build(ref systemState);
                systemState.RequireForUpdate(query);
            }

            var stateTypeIndex = TypeManager.GetTypeIndex<TState>();
            var instanceTypeIndex = TypeManager.GetTypeIndex<TInstance>();

            systemState.EntityManager.AddComponentData(systemState.SystemHandle, new StateInstance
            {
                State = stateTypeIndex,
                StateKey = stateKey,
                StateInstanceComponent = instanceTypeIndex,
            });
        }
    }
}
