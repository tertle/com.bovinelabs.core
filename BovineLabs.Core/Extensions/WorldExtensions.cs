namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core;
    using Unity.Entities;

    public static class WorldExtensions
    {
        public static bool IsThinClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        public static bool IsThinClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameThinClient) == WorldFlags.GameThinClient;
        }

        /// <summary>
        /// Includes thin clients.
        /// </summary>
        public static bool IsClientWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        /// <summary>
        /// Includes thin clients.
        /// </summary>
        public static bool IsClientWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameClient) == WorldFlags.GameClient || world.IsThinClientWorld();
        }

        public static bool IsClientOnlyWorld(this World world)
        {
            return world.Unmanaged.IsClientOnlyWorld();
        }

        public static bool IsClientOnlyWorld(this WorldUnmanaged world)
        {
            return world.IsClientWorld() && !world.IsServerWorld();
        }

        public static bool IsServerWorld(this World world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        public static bool IsServerWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.GameServer) == WorldFlags.GameServer;
        }

        public static bool IsServerLocalWorld(this World world)
        {
            return world.Unmanaged.IsServerLocalWorld();
        }

        public static bool IsServerLocalWorld(this WorldUnmanaged world)
        {
            return world.IsServerWorld() || world.IsLocalWorld();
        }

        public static bool IsEditorWorld(this World world)
        {
#if UNITY_EDITOR
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
#else
            return false;
#endif
        }

        public static bool IsEditorWorld(this WorldUnmanaged world)
        {
            return (world.Flags & WorldFlags.Editor) == WorldFlags.Editor;
        }
    }
}
