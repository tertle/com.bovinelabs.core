// <copyright file="AlwaysUpdatePhysicsWorldSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_PHYSICS_ALWAYS_UPDATE
namespace BovineLabs.Core.PhysicsUpdate
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using Unity.Jobs;
    using Unity.Physics;
    using Unity.Physics.Systems;

    [UpdateInGroup(typeof(BeforeTransformSystemGroup), OrderFirst = true)]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    public unsafe partial class AlwaysUpdatePhysicsWorldSystem : SystemBase
    {
        private SystemHandle buildPhysicsWorld;
        private SystemHandle buildPhysicsWorldDependencyResolver;
        private SystemState* buildPhysicsWorldSystemState;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            this.buildPhysicsWorld = this.World.GetExistingSystem<BuildPhysicsWorld>();
            this.buildPhysicsWorldDependencyResolver = this.World.GetExistingSystem<BuildPhysicsWorldDependencyResolver>();
            this.buildPhysicsWorldSystemState = this.World.Unmanaged.ResolveSystemStateChecked(this.buildPhysicsWorld);

            this.CheckedStateRef.AddDependency(TypeManager.GetTypeIndex<PhysicsWorldSingleton>());
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var physicsUpdated = SystemAPI.GetSingletonRW<AlwaysUpdatePhysicsWorld>();

            if (physicsUpdated.ValueRO.FixedStepUpdatedThisFrame)
            {
                physicsUpdated.ValueRW.FixedStepUpdatedThisFrame = false;
                return;
            }

            this.buildPhysicsWorldDependencyResolver.Update(this.World.Unmanaged);
            this.buildPhysicsWorld.Update(this.World.Unmanaged);

            var physicsDependency = this.buildPhysicsWorldSystemState->GetInternalDependency();
            this.Dependency = JobHandle.CombineDependencies(this.Dependency, physicsDependency);
        }
    }
}
#endif
