// <copyright file="DebugSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if !UNITY_NETCODE // When using NetCode we want to be able to insert this in the server as well
    [UpdateInGroup(typeof(PresentationSystemGroup))]
#endif
    public class DebugSystemGroup : ComponentSystemGroup
    {
    }
#endif
}