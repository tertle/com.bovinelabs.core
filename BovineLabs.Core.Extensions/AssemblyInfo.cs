// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
#if !BL_DISABLE_PHYSICS_STATES
using BovineLabs.Core.PhysicsStates;
using Unity.Jobs;
#endif

[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Tests")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.PerformanceTests")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Debug")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Authoring")]

#if !BL_DISABLE_PHYSICS_STATES
[assembly: RegisterGenericJobType(typeof(EnsureCurrentEventsCapacityJob<StatefulTriggerEvent, StatefulTriggerEventContainer>))]
[assembly: RegisterGenericJobType(typeof(EnsureCurrentEventsMapCapacityJob<StatefulTriggerEvent, StatefulTriggerEventContainer>))]
[assembly:
    RegisterGenericJobType(typeof(CollectEventsJob<StatefulTriggerEvent, StatefulTriggerEventContainer, StatefulTriggerEventSystem.CollectTriggerEvents>))]
[assembly: RegisterGenericJobType(typeof(CalculateEventMapBucketsJob<StatefulTriggerEvent, StatefulTriggerEventContainer>))]
[assembly: RegisterGenericJobType(typeof(CalculateCurrentEventsBucketsJob<StatefulTriggerEvent, StatefulTriggerEventContainer>))]

[assembly: RegisterGenericJobType(typeof(EnsureCurrentEventsCapacityJob<StatefulCollisionEvent, StatefulCollisionEventContainer>))]
[assembly: RegisterGenericJobType(typeof(EnsureCurrentEventsMapCapacityJob<StatefulCollisionEvent, StatefulCollisionEventContainer>))]
[assembly:
    RegisterGenericJobType(
        typeof(CollectEventsJob<StatefulCollisionEvent, StatefulCollisionEventContainer, StatefulCollisionEventSystem.CollectCollisionEvents>))]
[assembly: RegisterGenericJobType(typeof(CalculateEventMapBucketsJob<StatefulCollisionEvent, StatefulCollisionEventContainer>))]
[assembly: RegisterGenericJobType(typeof(CalculateCurrentEventsBucketsJob<StatefulCollisionEvent, StatefulCollisionEventContainer>))]
#endif
