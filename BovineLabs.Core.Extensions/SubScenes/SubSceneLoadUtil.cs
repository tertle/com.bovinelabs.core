// <copyright file="SubSceneLoadUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;

    public static class SubSceneLoadUtil
    {
        public static WorldFlags ConvertFlags(SubSceneLoadFlags targetWorld)
        {
            var flags = WorldFlags.None;

            if ((targetWorld & SubSceneLoadFlags.Game) != 0)
            {
                flags |= WorldFlags.Game;
            }

            if ((targetWorld & SubSceneLoadFlags.Service) != 0)
            {
                flags |= Worlds.ServiceWorld;
            }

#if UNITY_NETCODE
            if ((targetWorld & SubSceneLoadFlags.Client) != 0)
            {
                flags |= WorldFlags.GameClient;
            }

            if ((targetWorld & SubSceneLoadFlags.Server) != 0)
            {
                flags |= WorldFlags.GameServer;
            }

            if ((targetWorld & SubSceneLoadFlags.ThinClient) != 0)
            {
                flags |= WorldFlags.GameThinClient;
            }
#endif

            // Remove the live flag
            flags &= ~WorldFlags.Live;
            return flags;
        }
    }
}
#endif
