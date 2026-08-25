// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using Unity.Entities;

[assembly: DisableAutoTypeRegistration]
[assembly: InternalsVisibleTo("BovineLabs.Core.Editor")]

#if UNITY_PHYSICS
[assembly: RegisterUnityEngineComponentType(typeof(BovineLabs.Core.Authoring.Entities.RemovePhysicsVelocityAuthoring.RemovePhysicsVelocityBaking))]
#endif
