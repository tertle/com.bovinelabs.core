// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using Unity.Entities;

[assembly: DisableAutoTypeRegistration]

#if UNITY_PHYSICS
[assembly: RegisterUnityEngineComponentType(typeof(BovineLabs.Core.Authoring.Entities.RemovePhysicsVelocityAuthoring.RemovePhysicsVelocityBaking))]
#endif