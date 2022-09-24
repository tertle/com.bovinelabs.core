// <copyright file="ClientServerBootstrapInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE

namespace BovineLabs.Core.Internal
{
    using System;
    using System.Collections.Generic;
    using Unity.NetCode;

    public static class ClientServerBootstrapInternal
    {
        public static List<Type> ClientInitializationSystems => ClientServerBootstrap.s_State.ClientInitializationSystems;

        public static List<Type> ClientSimulationSystems => ClientServerBootstrap.s_State.ClientSimulationSystems;

        public static List<Type> ClientPresentationSystems => ClientServerBootstrap.s_State.ClientPresentationSystems;

        public static List<Type> ServerInitializationSystems => ClientServerBootstrap.s_State.ServerInitializationSystems;

        public static List<Type> ServerSimulationSystems => ClientServerBootstrap.s_State.ServerSimulationSystems;
    }
}

#endif
