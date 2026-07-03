// <copyright file="StartupSceneSwap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    public static class StartupSceneSwap
    {
        private static string sceneAssetPath = string.Empty;

        internal static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.ExitingEditMode:
                {
                    var scene = EditorSettingsUtility.GetSettings<EditorSettings>().StartupScene;
                    if (scene == null)
                    {
                        sceneAssetPath = string.Empty;
                        return;
                    }

                    sceneAssetPath = AssetDatabase.GetAssetPath(scene);
                    if (HasOpenSceneOnDisk())
                    {
                        EditorSceneManager.SaveOpenScenes();
                    }

                    return;
                }

                case PlayModeStateChange.EnteredPlayMode:
                {
                    if (!string.IsNullOrWhiteSpace(sceneAssetPath))
                    {
                        if (SceneManager.GetActiveScene().path != sceneAssetPath)
                        {
                            EditorSceneManager.LoadSceneInPlayMode(sceneAssetPath, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single });
                        }
                    }

                    return;
                }
            }
        }

        private static bool HasOpenSceneOnDisk()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
