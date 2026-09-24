namespace BovineLabs.Core.Editor.Utility
{
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class SampleEditorUtility
    {
        private const string PlayRequestKey = "BovineLabs.Samples.PlayScene";

        public static bool CanUseMenu => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling &&
            !EditorApplication.isUpdating && PrefabStageUtility.GetCurrentPrefabStage() == null;

        public static bool CanGenerateAutomatically
        {
            get
            {
                if (!CanUseMenu)
                {
                    return false;
                }

                for (var index = 0; index < SceneManager.sceneCount; index++)
                {
                    var scene = SceneManager.GetSceneAt(index);
                    if (scene.isDirty || (string.IsNullOrEmpty(scene.path) && scene.rootCount != 0))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static void Play(string scenePath, Action prepare)
        {
            if (!SaveScenesForGeneration(out var discardUnsavedScenes))
            {
                return;
            }

            if (discardUnsavedScenes)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            prepare();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                throw new InvalidOperationException($"The sample did not generate its main scene at '{scenePath}'.");
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SessionState.SetString(PlayRequestKey, scenePath);
            EditorApplication.isPlaying = true;
        }

        public static bool SaveScenesForGeneration(out bool discardUnsavedScenes)
        {
            discardUnsavedScenes = false;
            if (!CanUseMenu || Application.isBatchMode)
            {
                return false;
            }

            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Where(scene => scene.isDirty || (string.IsNullOrEmpty(scene.path) && scene.rootCount != 0)).ToArray();
            if (scenes.Length == 0)
            {
                return true;
            }

            if (EditorUtility.DisplayDialog("Save Open Scenes",
                    "The sample will temporarily close open scenes. Save modified and populated untitled scenes before continuing?",
                    "Save", "Don't Save"))
            {
                return EditorSceneManager.SaveScenes(scenes) && CanGenerateAutomatically;
            }

            discardUnsavedScenes = true;
            return CanUseMenu;
        }

        public static void RebuildGeneratedAssets(string generatedRoot, Action generate, bool discardUnsavedScenes = false)
        {
            var fullRoot = Path.GetFullPath(generatedRoot);
            var assetsRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!fullRoot.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(fullRoot) != "Generated")
            {
                throw new ArgumentException("The generated folder must be inside an imported sample under Assets.", nameof(generatedRoot));
            }

            if (!CanUseMenu || (discardUnsavedScenes && Application.isBatchMode) || (!discardUnsavedScenes && !CanGenerateAutomatically))
            {
                throw new InvalidOperationException("Save modified scenes, close populated untitled scenes and leave Play or Prefab Mode before generating.");
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            // Unload scenes before deleting their assets, and before creating assets that Single mode could unload.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                if (AssetDatabase.IsValidFolder(generatedRoot) && !AssetDatabase.DeleteAsset(generatedRoot))
                {
                    throw new IOException($"Could not replace generated sample folder '{generatedRoot}'.");
                }

                generate();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                // A failed generation can leave old scene paths absent. Keep its original exception visible and allow a retry.
                var existing = setup.Where(scene => !string.IsNullOrEmpty(scene.path) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null).ToArray();
                if (existing.Length > 0 && existing.Any(scene => scene.isLoaded))
                {
                    if (!existing.Any(scene => scene.isActive))
                    {
                        existing.First(scene => scene.isLoaded).isActive = true;
                    }

                    EditorSceneManager.RestoreSceneManagerSetup(existing);
                }
                else
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(PlayRequestKey, string.Empty)))
            {
                // Explicit sample launches take precedence after the host project's startup-scene handlers finish.
                EditorApplication.delayCall += OpenRequestedSample;
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(PlayRequestKey);
            }
        }

        private static void OpenRequestedSample()
        {
            var path = SessionState.GetString(PlayRequestKey, string.Empty);
            SessionState.EraseString(PlayRequestKey);
            if (EditorApplication.isPlaying && !string.IsNullOrEmpty(path) && SceneManager.GetActiveScene().path != path)
            {
                EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            }
        }
    }
}
