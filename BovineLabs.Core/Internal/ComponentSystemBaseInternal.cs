// <copyright file="ComponentSystemBaseInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class ComponentSystemBaseInternal
    {
        public static EntityQuery GetEntityQuery(this ComponentSystemBase system, params ComponentType[] componentTypes)
        {
            return system.GetEntityQuery(componentTypes);
        }

        public static EntityQuery GetEntityQuery(this ComponentSystemBase system, params EntityQueryDesc[] queryDesc)
        {
            return system.GetEntityQuery(queryDesc);
        }

        public static unsafe void RequireSingletonForUpdate(this ComponentSystemBase system, ComponentType componentType)
        {
            var state = system.CheckedState();
            var query = state->GetSingletonEntityQueryInternal(componentType);
            state->RequireForUpdate(query);
        }
    }
}
