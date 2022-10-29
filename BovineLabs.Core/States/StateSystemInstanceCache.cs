// <copyright file="StateSystemImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;

    internal static class StateSystemInstanceCache
    {
        public static NativeArray<StateSystemTypes> GetAllStateSystems(ref SystemState state)
        {
            if (!state.EntityManager.TryGetSingletonBuffer<StateSystemTypes>(out var buffer))
            {
                var entity = state.EntityManager.CreateEntity(typeof(StateSystemTypes));
                buffer = state.EntityManager.GetBuffer<StateSystemTypes>(entity);

                foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()))
                {
                    if (type.IsInterface || type.IsAbstract)
                    {
                        continue;
                    }

                    if ((type.IsClass && !typeof(SystemBase).IsAssignableFrom(type)) ||
                        (!type.IsClass && !typeof(ISystem).IsAssignableFrom(type)))
                    {
                        continue;
                    }

                    var systemHandle = state.World.GetExistingSystem(type);
                    if (!state.EntityManager.HasComponent<StateSystemInstance>(systemHandle))
                    {
                        continue;
                    }

                    var component = state.EntityManager.GetComponentData<StateSystemInstance>(systemHandle);
                    buffer.Add(new StateSystemTypes { Value = component });
                }
            }

            return buffer.AsNativeArray();
        }

        public struct StateSystemTypes : IBufferElementData
        {
            public StateSystemInstance Value;
        }
    }
}
