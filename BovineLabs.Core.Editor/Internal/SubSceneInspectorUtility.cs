// <copyright file="SubSceneInspectorUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using Unity.Scenes;

    public static class SubSceneInspectorUtility
    {
        public static void ForceReimport(params SubScene[] scenes)
        {
            Unity.Scenes.Editor.SubSceneInspectorUtility.ForceReimport(scenes);
        }
    }
}
