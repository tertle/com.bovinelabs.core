// // <copyright file="PhysicsWorldOutputDependencySystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// #if UNITY_PHYSICS
// namespace BovineLabs.Core.Physics
// {
//     using BovineLabs.Core.Collections;
//     using BovineLabs.Core.Extensions;
//     using Unity.Entities;
//     using Unity.Jobs;
//     using Unity.Physics.Systems;
//
//     // TODO REMOVE NO LONGER NEEDED
//     /// <summary> Handles creating a dependency handle on the physics world and update the physics singleton. </summary>
//     [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//     [UpdateAfter(typeof(EndFramePhysicsSystem))]
//     public unsafe partial class PhysicsWorldOutputDependencySystem : SystemBase
//     {
//         private BuildPhysicsWorld buildPhysicsWorld;
//         private SharedSafety sharedSafety;
//         private Entity collisionWorldEntity;
//         private ExportPhysicsWorld exportPhysicsWorld;
//
//         /// <inheritdoc/>
//         protected override void OnCreate()
//         {
//             this.buildPhysicsWorld = this.World.GetOrCreateSystem<BuildPhysicsWorld>();
//             this.exportPhysicsWorld = this.World.GetOrCreateSystem<ExportPhysicsWorld>();
//
//             this.sharedSafety = SharedSafety.Create();
//
//             // This is our safety handle for reading from the physics world
//             // By simply having the query in the system it'll add the necessary dependencies
//             this.GetEntityQuery(ComponentType.ReadWrite<CollisionWorldProxy>());
//
//             // EntityManager is used instead of GetSingletonEntity to avoid creating a second query in the the system
//             this.collisionWorldEntity = this.EntityManager.GetOrCreateSingletonEntity<CollisionWorldProxy>();
//
//             this.EntityManager.SetComponentData(
//                 this.collisionWorldEntity,
//                 new CollisionWorldProxy(this.buildPhysicsWorld.PhysicsWorld.CollisionWorld, this.sharedSafety.SafetyManager));
//         }
//
//         protected override void OnStartRunning()
//         {
//             this.RegisterPhysicsRuntimeSystemReadOnly();
//         }
//
//         /// <inheritdoc/>
//         protected override void OnDestroy()
//         {
//             this.sharedSafety.Dispose();
//         }
//
//         /// <inheritdoc/>
//         protected override void OnUpdate()
//         {
//             this.sharedSafety.Sync();
//
//             var entity = this.collisionWorldEntity;
//             var physicsWorld = this.buildPhysicsWorld.PhysicsWorld;
//             var shared = this.sharedSafety;
//
//             this.Job
//                 .WithCode(() =>
//                 {
//                     // SetComponent(entity, new CollisionWorldProxy(physicsWorld.CollisionWorld, shared.SafetyManager));
//                 })
//                 .WithReadOnly(physicsWorld)
//                 .Schedule();
//         }
//     }
// }
// #endif