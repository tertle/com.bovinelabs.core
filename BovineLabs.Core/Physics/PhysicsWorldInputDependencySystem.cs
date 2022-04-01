// // <copyright file="PhysicsWorldInputDependencySystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// #if UNITY_PHYSICS
// namespace BovineLabs.Core.Physics
// {
//     using Unity.Entities;
//     using Unity.Physics.Systems;
//
//     // TODO REMOVE
//     /// <summary> Handles automatically adding all read dependencies to the BuildPhysicsWorld system. </summary>
//     [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
//     [UpdateBefore(typeof(BuildPhysicsWorld))]
//     public partial class PhysicsWorldInputDependencySystem : SystemBase
//     {
//         private BuildPhysicsWorld buildPhysicsWorld;
//
//         /// <inheritdoc/>
//         protected override void OnCreate()
//         {
//             this.buildPhysicsWorld = this.World.GetOrCreateSystem<BuildPhysicsWorld>();
//
//             // This is our safety handle for finishing using the physics world before updating it.
//             // By simply having the query in the system it'll add the necessary dependencies
//             this.GetEntityQuery(ComponentType.ReadWrite<CollisionWorldProxy>());
//         }
//
//         protected override void OnStartRunning()
//         {
//             this.RegisterPhysicsRuntimeSystemReadWrite();
//         }
//
//         /// <inheritdoc/>
//         protected override void OnUpdate()
//         {
//             // this.buildPhysicsWorld.AddInputDependencyToComplete(this.Dependency);
//         }
//     }
// }
// #endif