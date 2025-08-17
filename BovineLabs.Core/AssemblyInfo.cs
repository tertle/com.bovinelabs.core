// <copyright file="AssemblyInfo.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Unity.Entities;
using UnityEngine;

[assembly: InternalsVisibleTo("BovineLabs.Core.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Authoring")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Debug")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Extensions.Editor")]
[assembly: InternalsVisibleTo("BovineLabs.Core.Tests")]
[assembly: InternalsVisibleTo("BovineLabs.Testing")]

[assembly:
    SuppressMessage("Code Quality", "CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.",
        Justification = "Unity")]

#if !HYBRID_ENTITIES_CAMERA_CONVERSION && UNITY_ENTITIES_1_4_0_pre_3
[assembly:RegisterUnityEngineComponentType(typeof(Camera))]
#endif