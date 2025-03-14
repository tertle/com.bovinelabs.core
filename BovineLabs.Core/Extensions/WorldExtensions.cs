// <copyright file="WorldExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    public static class WorldExtensions
    {
        /// <summary>
        /// Check if a world is a thin client.
        /// </summary>
        /// <param name="world"> A <see cref="World" /> instance </param>
        /// <returns> </returns>
        public static bool IsThinClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        /// <summary>
        /// Check if an unmanaged world is a thin client.
        /// </summary>
        /// <param name="world"> A <see cref="WorldUnmanaged" /> instance </param>
        /// <returns> </returns>
        public static bool IsThinClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        /// <summary>
        /// Check if a world is a client, will also return true for thin clients.
        /// </summary>
        /// <param name="world"> A <see cref="World" /> instance </param>
        /// <returns> </returns>
        public static bool IsClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        /// <summary>
        /// Check if an unmanaged world is a client, will also return true for thin clients.
        /// </summary>
        /// <param name="world"> A <see cref="WorldUnmanaged" /> instance </param>
        /// <returns> </returns>
        public static bool IsClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        /// <summary>
        /// Check if a world is a server.
        /// </summary>
        /// <param name="world"> A <see cref="World" /> instance </param>
        /// <returns> </returns>
        public static bool IsServerWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        /// <summary>
        /// Check if an unmanaged world is a server.
        /// </summary>
        /// <param name="world"> A <see cref="WorldUnmanaged" /> instance </param>
        /// <returns> </returns>
        public static bool IsServerWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        /// <summary> Check if a world is the editor world. </summary>
        /// <param name="world"> A <see cref="WorldUnmanaged" /> instance </param>
        /// <returns> </returns>
        public static bool IsEditorWorld(this World world)
        {
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }

        /// <summary> Check if an unmanaged world is the editor world. </summary>
        /// <param name="world"> A <see cref="WorldUnmanaged" /> instance </param>
        /// <returns> If it's an editor world. </returns>
        public static bool IsEditorWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }
    }
}
