// <copyright file="WorldExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    /// <summary>
    /// Extension helpers for identifying the role of a <see cref="World" /> instance.
    /// </summary>
    public static class WorldExtensions
    {
        /// <summary>
        /// Determines whether a world is a thin client.
        /// </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameThinClient" /> flag set.</returns>
        public static bool IsThinClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        /// <summary>
        /// Determines whether an unmanaged world is a thin client.
        /// </summary>
        /// <param name="world">The unmanaged world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameThinClient" /> flag set.</returns>
        public static bool IsThinClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        /// <summary>
        /// Determines whether a world is a client, including thin clients.
        /// </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameClient" /> flag or is a thin client.</returns>
        public static bool IsClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        /// <summary>
        /// Determines whether an unmanaged world is a client, including thin clients.
        /// </summary>
        /// <param name="world">The unmanaged world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameClient" /> flag or is a thin client.</returns>
        public static bool IsClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        /// <summary>
        /// Determines whether a world is a server.
        /// </summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameServer" /> flag set.</returns>
        public static bool IsServerWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        /// <summary>
        /// Determines whether an unmanaged world is a server.
        /// </summary>
        /// <param name="world">The unmanaged world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.GameServer" /> flag set.</returns>
        public static bool IsServerWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        /// <summary>Determines whether a world is the editor world.</summary>
        /// <param name="world">The world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.Editor" /> flag set.</returns>
        public static bool IsEditorWorld(this World world)
        {
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        /// <summary>Determines whether an unmanaged world is the editor world.</summary>
        /// <param name="world">The unmanaged world instance to check.</param>
        /// <returns>True if the world has the <see cref="WorldFlags.Editor" /> flag set.</returns>
        public static bool IsEditorWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }
    }
}
