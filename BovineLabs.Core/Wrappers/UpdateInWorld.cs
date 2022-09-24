// <copyright file="UpdateInWorld.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    [Flags]
    public enum TargetWorld
    {
        Default = 0,
        Client = 1,
        Server = 2,
        ClientAndServer = 3,
    }

    /// <summary> A replacement for Unity.NetCode.UpdateInWorld to not require dependencies. </summary>
    public class UpdateInWorld : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="UpdateInWorld"/> class. </summary>
        /// <param name="world"> The world to update in. </param>
        public UpdateInWorld(TargetWorld world)
        {
            this.World = world;
        }

        /// <summary> Gets the target world to update in. </summary>
        public TargetWorld World { get; }
    }
}
#endif
