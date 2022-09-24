// <copyright file="DebugSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if BL_GAME
    // When using Game we want to be able to insert this in the server as well so we manually move it to presentation on clients
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
#else
    [UpdateInGroup(typeof(PresentationSystemGroup))]
#endif
    [AlwaysUpdateSystem]
    public class DebugSystemGroup : ComponentSystemGroup
    {
        protected override void OnUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if UNITY_NETCODE
            if (this.HasSingleton<ThinClientComponent>())
            {
                return;
            }
#endif
#endif

            base.OnUpdate();
        }
    }
#endif

}
