// <copyright file="CameraMain.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CAMERA
namespace BovineLabs.Core.Camera
{
    using Unity.Entities;
    using Unity.NetCode;

    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    public struct CameraMain : IComponentData
    {
    }
}
#endif
