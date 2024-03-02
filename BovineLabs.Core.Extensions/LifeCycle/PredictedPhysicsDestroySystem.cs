// // <copyright file="PredictedPhysicsDestroySystem.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// #if !BL_DISABLE_LIFECYCLE && UNITY_PHYSICS
// namespace BovineLabs.Core.LifeCycle
// {
//     using BovineLabs.Core.Assertions;
//     using BovineLabs.Core.Extensions;
//     using Unity.Burst;
//     using Unity.Burst.Intrinsics;
//     using Unity.Entities;
//     using Unity.Physics;
//
//     [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
//     [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
//     public partial struct PredictedPhysicsDestroySystem : ISystem
//     {
//         private EntityQuery query;
//
//         /// <inheritdoc/>
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             // This system is only designed if you've overriden PhysicsVelocity and PhysicsCollider IEnableable
//             if (!ComponentType.ReadWrite<PhysicsVelocity>().IsEnableable || !ComponentType.ReadWrite<PhysicsCollider>().IsEnableable)
//             {
//                 state.Enabled = false;
//                 return;
//             }
//
//             this.query = SystemAPI.QueryBuilder()
//                 .WithAll<DestroyEntity, Simulate>()
//                 .WithAny<PhysicsVelocity, PhysicsCollider>()
//                 .Build();
//
//             this.query.SetChangedVersionFilter(ComponentType.ReadOnly<DestroyEntity>());
//         }
//
//         /// <inheritdoc/>
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             state.Dependency = new DisablePhysicsComponentsJob
//                 {
//                     PhysicsColliderHandle = SystemAPI.GetComponentTypeHandle<PhysicsCollider>(),
//                     PhysicsVelocityHandle = SystemAPI.GetComponentTypeHandle<PhysicsVelocity>(),
//                 }
//                 .ScheduleParallel(this.query, state.Dependency);
//         }
//
//         [BurstCompile]
//         private partial struct DisablePhysicsComponentsJob : IJobChunk
//         {
//             public ComponentTypeHandle<PhysicsCollider> PhysicsColliderHandle;
//             public ComponentTypeHandle<PhysicsVelocity> PhysicsVelocityHandle;
//
//             public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
//             {
//                 var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
//
//                 var hasCollider = chunk.Has(ref this.PhysicsColliderHandle);
//                 var hasVelocity = chunk.Has(ref this.PhysicsVelocityHandle);
//
//                 if (hasCollider)
//                 {
//                     if (hasVelocity)
//                     {
//                         var physicsColliders = chunk.GetEnabledMaskNoCheck(ref this.PhysicsColliderHandle);
//                         var physicsVelocities = chunk.GetEnabledMaskNoCheck(ref this.PhysicsVelocityHandle);
//
//                         while (enumerator.NextEntityIndex(out var index))
//                         {
//                             physicsColliders[index] = false;
//                             physicsVelocities[index] = false;
//                         }
//                     }
//                     else
//                     {
//                         var physicsColliders = chunk.GetEnabledMaskNoCheck(ref this.PhysicsColliderHandle);
//
//                         while (enumerator.NextEntityIndex(out var index))
//                         {
//                             physicsColliders[index] = false;
//                         }
//                     }
//                 }
//                 else
//                 {
//                     Check.Assume(hasVelocity);
//                     var physicsVelocities = chunk.GetEnabledMaskNoCheck(ref this.PhysicsVelocityHandle);
//
//                     while (enumerator.NextEntityIndex(out var index))
//                     {
//                         physicsVelocities[index] = false;
//                     }
//                 }
//             }
//         }
//     }
// }
// #endif
