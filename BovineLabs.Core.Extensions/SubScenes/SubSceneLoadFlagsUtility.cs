// <copyright file="SubSceneLoadFlagsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;

    public static class SubSceneLoadFlagsUtility
    {
        public static bool IncludeScene(this IBaker baker, SubSceneLoadFlags flags)
        {
#if UNITY_NETCODE
            if (baker.IsClient())
            {
                return (flags & (SubSceneLoadFlags.Client | SubSceneLoadFlags.ThinClient)) != 0;
            }

            if (baker.IsServer())
            {
                return (flags & SubSceneLoadFlags.Server) != 0;
            }
#endif
            return true;
        }
    }
}
#endif
