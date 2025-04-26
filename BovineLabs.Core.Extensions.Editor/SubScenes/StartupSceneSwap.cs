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

    [InitializeOnLoad]
    public static class StartupSceneSwap
    {
        private static string sceneAssetPath = string.Empty;

        static StartupSceneSwap()
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
                        return;
                    }

                    sceneAssetPath = AssetDatabase.GetAssetPath(scene);
                    var activeScene = SceneManager.GetActiveScene();

                    // If already active, don't need to load
                    if (sceneAssetPath == activeScene.path)
                    {
                        sceneAssetPath = string.Empty;
                    }
                    else
                    {
                        EditorSceneManager.SaveOpenScenes();
                    }

                    return;
                }

                case PlayModeStateChange.EnteredPlayMode:
                {
                    if (!string.IsNullOrWhiteSpace(sceneAssetPath))
                    {
                        EditorSceneManager.LoadSceneAsyncInPlayMode(sceneAssetPath, new LoadSceneParameters { loadSceneMode = LoadSceneMode.Single });
                    }

                    return;
                }

                default:
                {
                    sceneAssetPath = string.Empty;
                    return;
                }
            }
        }
    }
}
#endif
