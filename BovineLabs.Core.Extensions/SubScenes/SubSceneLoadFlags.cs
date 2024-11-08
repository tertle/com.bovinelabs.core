// <copyright file="SubSceneLoadFlags.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System;

    [Flags]
    public enum SubSceneLoadFlags : byte
    {
        Game = 1 << 0,
        Service = 1 << 1,
#if UNITY_NETCODE
        Client = 1 << 2,
        Server = 1 << 3,
        ThinClient = 1 << 4,
#endif
    }
}
#endif
