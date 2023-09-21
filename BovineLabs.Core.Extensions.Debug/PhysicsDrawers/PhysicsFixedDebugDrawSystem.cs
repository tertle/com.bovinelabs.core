// <copyright file="PhysicsFixedDebugDrawSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.Debug.PhysicsDrawers
{
    using BovineLabs.Draw;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Physics;
    using Unity.Physics.Systems;
    using UnityEngine;

    /// <summary> Debug drawing for physics that requires fixed update. </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct PhysicsFixedDebugDrawSystem : ISystem
    {
        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out PhysicsDebugDraw debug))
            {
                return;
            }

            if (debug is { DrawCollisionEvents: false, DrawTriggerEvents: false })
            {
                return;
            }

            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            var drawer = SystemAPI.GetSingleton<DrawSystem.Singleton>().CreateDrawer();

            if (debug.DrawCollisionEvents)
            {
                state.Dependency = new DrawCollisionEventsJob
                    {
                        World = physicsWorld,
                        Drawer = drawer,
                    }
                    .Schedule(simulation, state.Dependency);
            }

            if (debug.DrawTriggerEvents)
            {
                state.Dependency = new DrawTriggerEventsJob
                    {
                        World = physicsWorld,
                        Drawer = drawer,
                    }
                    .Schedule(simulation, state.Dependency);
            }
        }

        [BurstCompile]
        private struct DrawCollisionEventsJob : ICollisionEventsJob
        {
            [ReadOnly]
            public PhysicsWorld World;

            public Drawer Drawer;

            public void Execute(CollisionEvent collisionEvent)
            {
                var details = collisionEvent.CalculateDetails(ref this.World);

                // Color code the impulse depending on the collision feature
                // vertex - blue
                // edge - cyan
                // face - magenta
                Color color;
                switch (details.EstimatedContactPointPositions.Length)
                {
                    case 1:
                        color = Color.blue;
                        break;
                    case 2:
                        color = Color.cyan;
                        break;
                    default:
                        color = Color.magenta;
                        break;
                }

                var averageContactPosition = details.AverageContactPointPosition;

                this.Drawer.Point(averageContactPosition, 0.01f, color);
                this.Drawer.Arrow(averageContactPosition, collisionEvent.Normal * details.EstimatedImpulse, color);
            }
        }

        [BurstCompile]
        private struct DrawTriggerEventsJob : ITriggerEventsJob
        {
            [ReadOnly]
            public PhysicsWorld World;

            public Drawer Drawer;

            public void Execute(TriggerEvent triggerEvent)
            {
                var bodyA = this.World.Bodies[triggerEvent.BodyIndexA];
                var bodyB = this.World.Bodies[triggerEvent.BodyIndexB];

                var aabbA = bodyA.Collider.Value.CalculateAabb(bodyA.WorldFromBody);
                var aabbB = bodyB.Collider.Value.CalculateAabb(bodyB.WorldFromBody);

                this.Drawer.Line(aabbA.Center, aabbB.Center, Color.yellow);
            }
        }
    }
}
#endif
