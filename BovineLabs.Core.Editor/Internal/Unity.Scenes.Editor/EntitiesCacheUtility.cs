// <copyright file="EntitiesCacheUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    public static class EntitiesCacheUtility
    {
        public static void UpdateEntitySceneGlobalDependency()
        {
            Unity.Scenes.EntitiesCacheUtility.UpdateEntitySceneGlobalDependency();
        }
    }
}
