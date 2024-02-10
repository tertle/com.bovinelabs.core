// <copyright file="PhysicsToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.PhysicsDrawers;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct PhysicsToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<PhysicsToolbarBindings, PhysicsToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<PhysicsToolbarBindings, PhysicsToolbarBindings.Data>(ref state, "Physics", "physics");

            state.EntityManager.AddComponentData(state.SystemHandle, new PhysicsDebugDraw { DrawMeshColliderEdges = false });
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            this.toolbar.Unload();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            ref var data = ref this.toolbar.Binding;
            ref var c = ref state.EntityManager.GetComponentDataRW<PhysicsDebugDraw>(state.SystemHandle).ValueRW;
            c.DrawColliderEdges = data.DrawColliderEdges;
            c.DrawMeshColliderEdges = data.DrawMeshColliderEdges;
            c.DrawColliderAabbs = data.DrawColliderAabbs;
            c.DrawCollisionEvents = data.DrawCollisionEvents;
            c.DrawTriggerEvents = data.DrawTriggerEvents;
        }
    }
}
#endif
