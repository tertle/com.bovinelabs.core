// <copyright file="AlwaysUpdatePhysicsWorldSystem.cs" company="BovineLabs">
// Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_PHYSICS_ALWAYS_UPDATE
namespace BovineLabs.Core.PhysicsUpdate
{
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Physics.Systems;

    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    public unsafe partial class AlwaysUpdatePhysicsWorldSystem : SystemBase
    {
        private SystemHandle buildPhysicsWorld;
        private SystemState* buildPhysicsWorldSystemState;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.buildPhysicsWorld = this.World.GetExistingSystem<BuildPhysicsWorld>();
            this.buildPhysicsWorldSystemState = this.World.Unmanaged.ResolveSystemStateChecked(this.buildPhysicsWorld);

            this.CheckedStateRef.AddDependency(TypeManager.GetTypeIndex<PhysicsWorldSingleton>());
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var physicsUpdated = SystemAPI.GetSingletonRW<AlwaysUpdatePhysicsWorld>();

            if (physicsUpdated.ValueRO.FixedStepUpdatedThisFrame)
            {
                physicsUpdated.ValueRW.FixedStepUpdatedThisFrame = false;
                return;
            }

            this.buildPhysicsWorld.Update(this.World.Unmanaged);

            var physicsDependency = this.buildPhysicsWorldSystemState->GetInternalDependency();
            this.Dependency = JobHandle.CombineDependencies(this.Dependency, physicsDependency);
        }
    }
}
#endif
