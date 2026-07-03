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
            if (!baker.IsBakingForEditor())
            {
                if (baker.IsClient() && baker.IsServer())
                {
                    return true;
                }

                const SubSceneLoadFlags networkFlags = SubSceneLoadFlags.Server | SubSceneLoadFlags.Client | SubSceneLoadFlags.ThinClient;

                if (baker.IsClient())
                {
                    // Ignore things that only appear on server
                    return (flags & networkFlags) != SubSceneLoadFlags.Server;
                }

                if (baker.IsServer())
                {
                    var match = flags & networkFlags;

                    // Ignore things that only appear on client
                    return match != SubSceneLoadFlags.Client &&
                        match != SubSceneLoadFlags.ThinClient &&
                        match != (SubSceneLoadFlags.Client | SubSceneLoadFlags.ThinClient);
                }
            }
#endif
            return true;
        }
    }
}
#endif
