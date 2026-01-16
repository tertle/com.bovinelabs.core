// <copyright file="Worlds.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using Unity.NetCode;

    public static class Worlds
    {
        public const WorldSystemFilterFlags Service = (WorldSystemFilterFlags)(1 << 21);

        public const WorldSystemFilterFlags ClientLocal = WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation;

        public const WorldSystemFilterFlags ServerLocal = WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation;

        public const WorldSystemFilterFlags ServerLocalEditor = ServerLocal | WorldSystemFilterFlags.Editor;

        public const WorldSystemFilterFlags Simulation =
            WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation;

        public const WorldSystemFilterFlags SimulationService = Simulation | Service;

        public const WorldSystemFilterFlags SimulationEditor = Simulation | WorldSystemFilterFlags.Editor;

        public const WorldSystemFilterFlags All = SimulationEditor | Service;

        public const WorldFlags ServiceWorld = (WorldFlags)(1 << 16) | WorldFlags.Live;

        /// <summary> Determines whether a world is a service world. </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="Worlds.ServiceWorld" /> flag.</returns>
        public static bool IsServiceWorld(this World world)
        {
            return world.Unmanaged.IsServiceWorld();
        }

        /// <summary> Determines whether a world is a service world. </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="Worlds.ServiceWorld" /> flag.</returns>
        public static bool IsServiceWorld(this WorldUnmanaged world)
        {
            return (world.Flags & ServiceWorld) == ServiceWorld;
        }

        /// <summary> Determines whether a world is a service world. </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="Worlds.ServiceWorld" /> flag.</returns>
        public static bool IsLocalWorld(this World world)
        {
            return world.Unmanaged.IsLocalWorld();
        }

        /// <summary> Determines whether a world is a service world. </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="Worlds.ServiceWorld" /> flag.</returns>
        public static bool IsLocalWorld(this WorldUnmanaged world)
        {
            // Make sure it's a game world (eliminates service + anything else custom)
            if ((world.Flags & WorldFlags.Game) != WorldFlags.Game)
            {
                return false;
            }

            return !world.IsClientWorld() && !world.IsThinClientWorld() && !world.IsServerWorld();
        }
    }
}
