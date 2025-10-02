// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;
using BovineLabs.Core.Authoring.Blobs;
using Unity.Entities;

[assembly: DisableAutoTypeRegistration]

[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Tests")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.PerformanceTests")]

[assembly: RegisterUnityEngineComponentType(typeof(EntityBlobBakedData))]