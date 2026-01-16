// <copyright file="BovineLabsBootstrap.NetCode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.NetCode;
    using Unity.Networking.Transport;
    using UnityEngine.Assertions;

    /// <summary> Bootstrap for Unity NetCode games. </summary>
    public abstract partial class BovineLabsBootstrap : ClientServerBootstrap
    {
        private const string AddressName = "network.address";
        private const string AddressDesc = "Network address to connect to.";

        private const string PortName = "network.port";
        private const string PortDesc = "Network port to connect to.";

        [ConfigVar(AddressName, "127.0.0.1", AddressDesc, true)]
        public static readonly SharedStatic<FixedString32Bytes> AddressVar = SharedStatic<FixedString32Bytes>.GetOrCreate<AddressType>();

        [ConfigVar(PortName, 7979, PortDesc)]
        public static readonly SharedStatic<int> Port = SharedStatic<int>.GetOrCreate<PortType>();

        [ConfigVar("network.require-connection-approval", false, "Is connection approval required to connect to a netcode service", true)]
        public static readonly SharedStatic<bool> RequireConnectionApproval = SharedStatic<bool>.GetOrCreate<RequireConnectionApprovalType>();

        /// <summary> Creates the client and server world as long as we aren't dedicated. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a server or client world. </exception>
        /// <param name="isLocal"> Should the game be local. </param>
        public void CreateClientServerWorlds(bool isLocal)
        {
            var requestedPlayType = RequestedPlayType;

#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST && !UNITY_CLIENT && !UNITY_SERVER
            if (NetCodeConfig.Global != null && NetCodeConfig.Global.HostWorldModeSelection == NetCodeConfig.HostWorldMode.SingleWorld &&
                requestedPlayType == PlayType.ClientAndServer)
            {
                this.CreateSingleWorldHost(isLocal);
            }
            else
#endif
            {
#if !UNITY_CLIENT
                if (requestedPlayType != PlayType.Client)
                {
                    this.CreateServerWorldInternal(isLocal);
                }
#endif

#if !UNITY_SERVER
                if (requestedPlayType != PlayType.Server)
                {
                    this.CreateClientWorld();
                }
#endif
            }
        }

        /// <summary> Destroys the client and server worlds if they exist. </summary>
        public void DestroyClientServerWorlds()
        {
            World.DefaultGameObjectInjectionWorld = ServiceWorld;

#if !UNITY_CLIENT
            this.DestroyServerWorld();
#endif

#if !UNITY_SERVER
            this.DestroyClientWorld();
#endif
        }

#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST && !UNITY_CLIENT && !UNITY_SERVER
        /// <summary> Creates a client server host world. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a server world. </exception>
        /// <param name="isLocal"> Should the game be local. </param>
        public void CreateSingleWorldHost(bool isLocal)
        {
            if (ServerWorld != null)
            {
                throw new InvalidOperationException("ServerWorld has not been correctly cleaned up");
            }

            if (ClientWorld != null)
            {
                throw new InvalidOperationException("ClientWorld has not been correctly cleaned up");
            }

            var clientServerWorld = CreateSingleWorldHost("ClientAndServerWorld");

            World.DefaultGameObjectInjectionWorld = clientServerWorld; // replace default injection world
            InitializeWorld(clientServerWorld);
            InitializeNetCodeWorld(clientServerWorld);
            this.ServerListen(clientServerWorld, isLocal);
        }
#endif

#if !UNITY_CLIENT
        /// <summary> Creates a server world. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a server world. </exception>
        public void CreateServerWorld()
        {
            this.CreateServerWorldInternal(false);
        }

        /// <summary> Destroys all server worlds. </summary>
        public void DestroyServerWorld()
        {
            for (var index = ServerWorlds.Count - 1; index >= 0; index--)
            {
                ServerWorlds[index].Dispose();
            }
        }
#endif

#if !UNITY_SERVER
        /// <summary> Creates a client world. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a client world. </exception>
        public void CreateClientWorld()
        {
            if (ClientWorld != null)
            {
                throw new InvalidOperationException("ClientWorld has not been correctly cleaned up");
            }

            var world = CreateClientWorld("ClientWorld");
            Assert.IsNotNull(ClientWorld);

#if !UNITY_CLIENT
            // Only override default injection if no server world
            if (World.DefaultGameObjectInjectionWorld != ServerWorld)
#endif
            {
                World.DefaultGameObjectInjectionWorld = world;
            }

            InitializeWorld(world);
            InitializeNetCodeWorld(world);
            this.ClientConnect(world);
        }

        /// <summary> Creates a new thin client world world. </summary>
        public new void CreateThinClientWorld()
        {
            var world = ClientServerBootstrap.CreateThinClientWorld();

            InitializeWorld(world);
            InitializeNetCodeWorld(world);
            this.ClientConnect(world);
        }

        /// <summary> Destroy all client and thin client worlds. </summary>
        public void DestroyClientWorld()
        {
            for (var index = ClientWorlds.Count - 1; index >= 0; index--)
            {
                ClientWorlds[index].Dispose();
            }

            for (var index = ThinClientWorlds.Count - 1; index >= 0; index--)
            {
                ThinClientWorlds[index].Dispose();
            }
        }
#endif

#if !UNITY_CLIENT
        private void CreateServerWorldInternal(bool isLocal)
        {
            if (ServerWorld != null)
            {
                throw new InvalidOperationException("ServerWorld has not been correctly cleaned up");
            }

            var world = CreateServerWorld("ServerWorld");
            Assert.IsNotNull(ServerWorld);

            World.DefaultGameObjectInjectionWorld = world; // replace default injection world
            InitializeWorld(world);
            InitializeNetCodeWorld(world);
            this.ServerListen(world, isLocal);
        }

        private void ServerListen(World world, bool isLocal)
        {
            var ep = GetNetworkEndpoint(isLocal);
            var driver = world.EntityManager.GetSingletonRW<NetworkStreamDriver>();
            if (RequireConnectionApproval.Data)
            {
                driver.ValueRW.RequireConnectionApproval = true;
            }

            driver.ValueRW.Listen(ep);
        }
#endif

#if !UNITY_SERVER
        private void ClientConnect(World world)
        {
            var ep = GetNetworkEndpoint(false);
            var driver = world.EntityManager.GetSingletonRW<NetworkStreamDriver>();
            if (RequireConnectionApproval.Data)
            {
                driver.ValueRW.RequireConnectionApproval = true;
            }

            driver.ValueRW.Connect(world.EntityManager, ep);
        }
#endif

        private static NetworkEndpoint GetNetworkEndpoint(bool isLocal)
        {
            if (isLocal)
            {
                return NetworkEndpoint.LoopbackIpv4.WithPort(7979);
            }

            return NetworkEndpoint.TryParse(AddressVar.Data.ToString(), (ushort)Port.Data, out var endpoint)
                ? endpoint
                : NetworkEndpoint.LoopbackIpv4.WithPort(7979);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private static void InitializeNetCodeWorld(World world)
        {
            // This allows sending RPCs between a stand alone build and the editor for testing purposes in the event when you finish this example
            // you want to connect a server-client stand alone build to a client configured editor instance.
            world.EntityManager.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
        }

        private struct AddressType
        {
        }

        private struct PortType
        {
        }

        private struct RequireConnectionApprovalType
        {
        }
    }
}
#endif
