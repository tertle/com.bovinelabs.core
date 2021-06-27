// <copyright file="ExecuteInWorld.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;

    /// <summary> The target world flag enum that defines where systems and settings should be added. </summary>
    [Flags]
    public enum Worlds
    {
        /// <summary> Update in default world. </summary>
        Default = 0,

        /// <summary> Update in client world. </summary>
        Client = 1,

        /// <summary> Update in client world. </summary>
        Server = 2,

        /// <summary> Update in client world. </summary>
        ClientAndServer = 3,

        /// <summary> Update in default world, this is what adds support. </summary>
        DefaultExplicit = 4,

        /// <summary> Update in default and client worlds. </summary>
        DefaultAndClient = DefaultExplicit | Client,

        /// <summary> Update in default, client adn server worlds. </summary>
        DefaultClientAndServer = DefaultExplicit | ClientAndServer,
    }

#if UNITY_NETCODE
    /// <summary>
    /// A replacement for <see cref="Unity.NetCode.UpdateInWorld"/> that allows specifying default world in combination of game worlds.
    /// This can be applied to systems or settings.
    /// </summary>
    /// <remarks>
    /// This inherits from <see cref="Unity.NetCode.UpdateInWorld"/> and using that instead will work for systems.
    /// </remarks>
    public class ExecuteInWorld : Unity.NetCode.UpdateInWorld
    {
        /// <summary> Initializes a new instance of the <see cref="ExecuteInWorld"/> class. </summary>
        /// <param name="world"> The world to update in. </param>
        public ExecuteInWorld(Worlds world)
            : base((Unity.NetCode.UpdateInWorld.TargetWorld)world & Unity.NetCode.UpdateInWorld.TargetWorld.ClientAndServer)
        {
            this.World = world;
        }
#else
    /// <summary>
    /// A replacement for Unity.NetCode.UpdateInWorld that allows specifying default world in combination of game worlds.
    /// This can be applied to systems or settings.
    /// </summary>
    public class ExecuteInWorld : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="ExecuteInWorld"/> class. </summary>
        /// <param name="world"> The world to update in. </param>
        public ExecuteInWorld(Worlds world)
        {
            this.World = world;
        }
#endif

        /// <summary> Gets the target world to update in. </summary>
        public new Worlds World { get; }
    }
}