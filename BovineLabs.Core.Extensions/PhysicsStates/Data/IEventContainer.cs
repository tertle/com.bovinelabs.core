// <copyright file="IEventContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PHYSICS_STATES
namespace BovineLabs.Core.PhysicsStates
{
    using System;
    using Unity.Entities;
    using Unity.Physics;

    public interface IEventContainer<T, TC> : IEquatable<TC>
        where TC : unmanaged, IEventContainer<T, TC>
        where T : unmanaged, IBufferElementData, ISimulationEvent<T>
    {
        T Create(StatefulEventState state);

        Entity EntityA { get; }

        Entity EntityB { get; }
    }
}
#endif
