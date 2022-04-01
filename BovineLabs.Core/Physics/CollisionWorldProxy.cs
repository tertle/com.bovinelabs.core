// // <copyright file="CollisionWorldProxy.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// #if UNITY_PHYSICS
// namespace BovineLabs.Core.Physics
// {
//     using System.Diagnostics.CodeAnalysis;
//     using System.Runtime.InteropServices;
//     using BovineLabs.Core.Collections;
//     using JetBrains.Annotations;
//     using Unity.Burst;
//     using Unity.Collections;
//     using Unity.Collections.LowLevel.Unsafe;
//     using Unity.Entities;
//     using Unity.Physics;
//     using AtomicSafetyManager = BovineLabs.Core.Collections.AtomicSafetyManager;
//
//     /// <summary> A singleton component proxy for the Physics <see cref="CollisionWorld"/> which allows automatic safety handling. </summary>
//     public unsafe struct CollisionWorldProxy : IComponentData
//     {
//         private readonly NativeArrayProxy<RigidBody> bodies;
//         private readonly BroadPhaseTreeProxy staticTree;
//         private readonly BroadPhaseTreeProxy dynamicTree;
//         private readonly NativeHashMapProxy<Entity, int> entityBodyIndexMap;
//
//         [NativeDisableUnsafePtrRestriction]
//         private readonly AtomicSafetyManager* safetyManager;
//
//         internal CollisionWorldProxy(CollisionWorld collisionWorld, AtomicSafetyManager* safetyManager)
//         {
//             this.bodies = new NativeArrayProxy<RigidBody>(collisionWorld.Bodies);
//             this.staticTree = new BroadPhaseTreeProxy(collisionWorld.Broadphase.StaticTree);
//             this.dynamicTree = new BroadPhaseTreeProxy(collisionWorld.Broadphase.DynamicTree);
//             this.entityBodyIndexMap = new NativeHashMapProxy<Entity, int>(collisionWorld.EntityBodyIndexMap);
//             this.safetyManager = safetyManager;
//         }
//
//         /// <summary> Gets the collision world. </summary>
//         /// <returns> The collision world with safety. </returns>
//         public CollisionWorld ToCollisionWorld()
//         {
//             return CollisionWorldFactory.Create(
//                 this.bodies.ToArray(this.safetyManager),
//                 new Broadphase(this.staticTree.ToTree(this.safetyManager), this.dynamicTree.ToTree(this.safetyManager)),
//                 this.entityBodyIndexMap.ToNativeHashMap(this.safetyManager));
//         }
//
//         // Because CollisionWorld.m_Bodies is private we have to be creative with creating it.
//         // The CollisionWorld(NativeArray<RigidBody> bodies, Broadphase broadphase) constructor could be used but that would require
//         // allocating a hashmap then disposing it straight away before setting EntityBodyIndexMap.
//         // Instead a simple memory remap is used to avoid the need to allocate.
//         [StructLayout(LayoutKind.Explicit)]
//         private readonly struct CollisionWorldFactory
//         {
//             [FieldOffset(0)]
//             [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "FieldOffset")]
//             private readonly CollisionWorldImposter imposter;
//
//             [FieldOffset(0)]
//             private readonly CollisionWorld collisionWorld;
//
//             private CollisionWorldFactory(CollisionWorldImposter imposter)
//                 : this()
//             {
//                 this.imposter = imposter;
//             }
//
//             public static CollisionWorld Create(NativeArray<RigidBody> bodies, Broadphase broadphase, NativeHashMap<Entity, int> entityBodyIndexMap)
//             {
//                 return new CollisionWorldFactory(new CollisionWorldImposter
//                     {
//                         Bodies = bodies,
//                         Broadphase = broadphase,
//                         EntityBodyIndexMap = entityBodyIndexMap,
//                     })
//                     .collisionWorld;
//             }
//
//             private struct CollisionWorldImposter
//             {
//                 [NoAlias]
//                 [UsedImplicitly]
//                 public NativeArray<RigidBody> Bodies;
//
//                 [NoAlias]
//                 [UsedImplicitly]
//                 public Broadphase Broadphase;
//
//                 [NoAlias]
//                 [UsedImplicitly]
//                 public NativeHashMap<Entity, int> EntityBodyIndexMap;
//             }
//         }
//
//         private readonly struct BroadPhaseTreeProxy
//         {
//             private readonly NativeArrayProxy<BoundingVolumeHierarchy.Node> nodes;
//             private readonly NativeArrayProxy<CollisionFilter> nodeFilters;
//             private readonly NativeArrayProxy<CollisionFilter> bodyFilters;
//             private readonly NativeArrayProxy<bool> respondsToCollision;
//             private readonly NativeArrayProxy<BoundingVolumeHierarchy.Builder.Range> ranges;
//             private readonly NativeArrayProxy<int> branchCount;
//
//             public BroadPhaseTreeProxy(Broadphase.Tree tree)
//             {
//                 this.nodes = new NativeArrayProxy<BoundingVolumeHierarchy.Node>(tree.Nodes);
//                 this.nodeFilters = new NativeArrayProxy<CollisionFilter>(tree.NodeFilters);
//                 this.bodyFilters = new NativeArrayProxy<CollisionFilter>(tree.BodyFilters);
//                 this.respondsToCollision = new NativeArrayProxy<bool>(tree.RespondsToCollision);
//                 this.ranges = new NativeArrayProxy<BoundingVolumeHierarchy.Builder.Range>(tree.Ranges);
//                 this.branchCount = new NativeArrayProxy<int>(tree.BranchCount);
//             }
//
//             public Broadphase.Tree ToTree(AtomicSafetyManager* safetyManager)
//             {
//                 return new Broadphase.Tree
//                 {
//                     BranchCount = this.branchCount.ToArray(safetyManager),
//                     Nodes = this.nodes.ToArray(safetyManager),
//                     NodeFilters = this.nodeFilters.ToArray(safetyManager),
//                     RespondsToCollision = this.respondsToCollision.ToArray(safetyManager),
//                     BodyFilters = this.bodyFilters.ToArray(safetyManager),
//                     Ranges = this.ranges.ToArray(safetyManager),
//                 };
//             }
//         }
//     }
// }
// #endif