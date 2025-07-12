// <copyright file="EditorSceneUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using UnityEditor;
    using UnityEngine.SceneManagement;

    public static class EditorSceneUtil
    {
        public static bool IsSceneAssetOpen(SceneAsset sceneAsset)
        {
            if (!sceneAsset)
            {
                return false;
            }

            var scenePath = AssetDatabase.GetAssetPath(sceneAsset);

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == scenePath && scene.isLoaded)
                {
                    return true;
                }
            }

            return false;
        }
    }
}