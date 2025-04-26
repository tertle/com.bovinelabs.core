// <copyright file="BovineLabsBootstrap.NetCode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core
{
    using System;
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
        public void CreateClientServerWorlds()
        {
            var requestedPlayType = RequestedPlayType;
#if !UNITY_CLIENT
            if (requestedPlayType != PlayType.Client)
            {
                this.CreateServerWorld();
            }
#endif

#if !UNITY_SERVER
            if (requestedPlayType != PlayType.Server)
            {
                this.CreateClientWorld();
            }
#endif
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

#if !UNITY_CLIENT
        /// <summary> Creates a server world. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a server world. </exception>
        public void CreateServerWorld()
        {
            if (ServerWorld != null)
            {
                throw new InvalidOperationException("ServerWorld has not been correctly cleaned up");
            }

            var world = CreateServerWorld("ServerWorld");
            WorldAllocator.CreateAllocator(world.Unmanaged.SequenceNumber);
            Assert.IsNotNull(ServerWorld);

            World.DefaultGameObjectInjectionWorld = world; // replace default injection world
            InitializeWorld(world);
            InitializeNetCodeWorld(world);
            this.ServerListen(world);
        }

        /// <summary> Destroys all server worlds. </summary>
        public void DestroyServerWorld()
        {
            for (var index = ServerWorlds.Count - 1; index >= 0; index--)
            {
                DisposeWorld(ServerWorlds[index]);
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
            WorldAllocator.CreateAllocator(world.Unmanaged.SequenceNumber);
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
            WorldAllocator.CreateAllocator(world.Unmanaged.SequenceNumber);

            InitializeWorld(world);
            InitializeNetCodeWorld(world);
            this.ClientConnect(world);
        }

        /// <summary> Destroy all client and thin client worlds. </summary>
        public void DestroyClientWorld()
        {
            for (var index = ClientWorlds.Count - 1; index >= 0; index--)
            {
                DisposeWorld(ClientWorlds[index]);
            }

            for (var index = ThinClientWorlds.Count - 1; index >= 0; index--)
            {
                DisposeWorld(ThinClientWorlds[index]);
            }
        }
#endif

#if !UNITY_CLIENT
        private void ServerListen(World world)
        {
            var ep = GetNetworkEndpoint();
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
            var ep = GetNetworkEndpoint();
            var driver = world.EntityManager.GetSingletonRW<NetworkStreamDriver>();
            if (RequireConnectionApproval.Data)
            {
                driver.ValueRW.RequireConnectionApproval = true;
            }

            driver.ValueRW.Connect(world.EntityManager, ep);
        }
#endif

        private static NetworkEndpoint GetNetworkEndpoint()
        {
            return NetworkEndpoint.TryParse(AddressVar.Data.ToString(), (ushort)Port.Data, out var endpoint)
                ? endpoint
                : NetworkEndpoint.LoopbackIpv4.WithPort(7979);
        }

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
