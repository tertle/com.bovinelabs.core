// <copyright file="StateImpl.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.States
{
    using System;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Logging;

    public struct StateImpl : IDisposable
    {
        public readonly EntityQuery Query;

        public EntityTypeHandle EntityType;
        public DynamicComponentTypeHandle StateType;
        public DynamicComponentTypeHandle PreviousStateType;

        public NativeParallelHashMap<byte, ComponentType> RegisteredStatesMap;

        public StateImpl(ref SystemState state, ComponentType stateComponent, ComponentType previousStateComponent)
        {
            this.Query = new EntityQueryBuilder(Allocator.Temp).WithAll(stateComponent).WithAllRW(previousStateComponent).Build(ref state);
            this.Query.AddChangedVersionFilter(stateComponent);

            this.EntityType = state.GetEntityTypeHandle();
            this.StateType = state.GetDynamicComponentTypeHandle(stateComponent);
            this.PreviousStateType = state.GetDynamicComponentTypeHandle(previousStateComponent);

            this.RegisteredStatesMap = new NativeParallelHashMap<byte, ComponentType>(256, Allocator.Persistent);

            var stateSystems = StateInstanceCache.GetAllStateSystems(ref state);

            foreach (var component in stateSystems)
            {
                if (component.Value.State != this.StateType.m_TypeIndex)
                {
                    continue;
                }

                var stateInstanceComponent = ComponentType.FromTypeIndex(component.Value.StateInstanceComponent);

                if (!this.RegisteredStatesMap.TryAdd(component.Value.StateKey, stateInstanceComponent))
                {
                    Log.Error($"Key {component.Value.StateKey} has already been registered");
                }
            }
        }

        public void Update(ref SystemState state)
        {
            this.EntityType.Update(ref state);
            this.StateType.Update(ref state);
            this.PreviousStateType.Update(ref state);
        }

        public void Dispose()
        {
            this.RegisteredStatesMap.Dispose();
        }
    }
}
