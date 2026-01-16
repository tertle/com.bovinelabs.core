// <copyright file="SubSceneEditorToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE && UNITY_6000_3_OR_NEWER
namespace BovineLabs.Core.Editor.SubScenes
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Editor.Utility;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.SubScenes;
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;
    using Object = UnityEngine.Object;
    using SubSceneUtility = Unity.Scenes.Editor.SubSceneUtility;

    [Configurable]
    [UsedImplicitly]
    public static class SubSceneEditorToolbar
    {
        private const string ScenePath = "BovineLabs/Scene";
        private const string SceneSetPath = "BovineLabs/SceneSet";

        private const string ScenePlayModeTooltip = "Open a set of scenes as live baking without the need for a SubScene";
        private const string SceneEditModeTooltip = "Left click to open a scene for editing or right click to open a scene as a subscene";

        private static readonly Dictionary<Hash128, SubScene> TempSubScenes = new();
        private static readonly Dictionary<SceneAsset, SubScene> EditorSubScenes = new();

        private static MainToolbarDropdown? sceneDropDown;

        static SubSceneEditorToolbar()
        {
            ConfigVarManager.Initialize();
            SceneManager.sceneLoaded += (_, _) => CleanupOldSubScenes();
            EditorApplication.playModeStateChanged += SceneOnPlayModeStatusChanged;
        }

        private delegate bool AddSetDelegate<in T>(T dropDown, SubSceneSetBase set, string setPath)
            where T : IDropDown;

        private delegate void AddSceneDelegate<in T>(T dropDown, SceneAsset scene, string setName)
            where T : IDropDown;

        [UsedImplicitly]
        [MainToolbarPreset]
        [MainToolbarElement(ScenePath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement SceneSelection()
        {
            var icon = (Texture2D)EditorGUIUtility.IconContent("PreMatCube").image;
            var content = new MainToolbarContent(icon) { tooltip = GetTooltip() };
            sceneDropDown = new MainToolbarDropdown(content, rect => SceneSelectionClicked(new GenericMenuWrapper(rect),  false))
            {
                // right click action
                populateContextMenu = menu => SceneSelectionClicked(new DropdownMenuWrapper(menu), true),
            };

            return sceneDropDown;
        }

        private static string GetTooltip()
        {
            return EditorApplication.isPlaying ? ScenePlayModeTooltip : SceneEditModeTooltip;
        }

        private static void SceneOnPlayModeStatusChanged(PlayModeStateChange obj)
        {
            CleanupOldSubScenes();

            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    MainToolbar.Refresh(ScenePath);
                    MainToolbar.Refresh(SceneSetPath);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    foreach (var s in EditorSubScenes)
                    {
                        if (s.Value)
                        {
                            Object.DestroyImmediate(s.Value.gameObject);
                        }
                    }

                    EditorSubScenes.Clear();
                    break;
            }
        }

        private static void CleanupOldSubScenes()
        {
            foreach (var s in TempSubScenes.ToArray())
            {
                if (!s.Value)
                {
                    var scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(s.Key));
                    SceneManager.UnloadSceneAsync(scene);
                    TempSubScenes.Remove(s.Key);
                }
            }
        }

        private static void SceneSelectionClicked<T>(T worldBound, bool baking)
            where T : IDropDown
        {
            if (EditorApplication.isPlaying)
            {
                if (baking)
                {
                    return;
                }

                LivingBakingDropdown(worldBound);
            }
            else
            {
                if (baking)
                {
                    SceneSelectionDropDown(worldBound, (_, _, _) => true, AddSceneBake);
                }
                else
                {
                    SceneSelectionDropDown(worldBound, AddSetOpen, AddSceneOpen);
                }
            }
        }

        private static void SceneSelectionDropDown<T>(T dropDown, AddSetDelegate<T> addSet, AddSceneDelegate<T> addScene)
            where T : IDropDown
        {
            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();

            var sets = settings.SceneSets.Cast<SubSceneSetBase>().Concat(settings.EditorSceneSets).Where(s => s && s.Scenes.Count > 0).OrderBy(s => s.name).ToArray();
            var shorten = sets.Length > 20;

            foreach (var set in sets)
            {
                var setName = set.name.ToSentence();
                var setPath = setName;

                if (shorten)
                {
                    setPath = $"{setPath.FirstCharToUpper()[0]}/{setPath}";
                }

                if (!addSet(dropDown, set, setPath))
                {
                    continue;
                }

                var scenes = set
                    .Scenes
                    .Where(scene => scene)
                    .Concat(EditorBuildSettings.scenes.Select(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)))
                    .Distinct()
                    .Where(sa => sa)
                    .OrderBy(sa => sa.name);

                foreach (var scene in scenes)
                {
                    addScene(dropDown, scene, setPath);
                }
            }

            dropDown.Finish();
        }

        private static bool AddSetOpen<T>(T dropDown, SubSceneSetBase set, string setPath)
            where T : IDropDown
        {
            var scenes = set.Scenes.Where(s => s).ToArray();
            if (scenes.Length == 0)
            {
                return false;
            }

            dropDown.AddItem(setPath + "/Open All", false, static data =>
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                var set = (SubSceneSetBase)data;
                var scenes = set.Scenes.Where(s => s).ToArray();
                if (scenes.Length == 0)
                {
                    return;
                }

                // Open the first scene
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scenes[0]), OpenSceneMode.Single);
                for (var i = 1; i < scenes.Length; i++)
                {
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scenes[i]), OpenSceneMode.Additive);
                }
            }, set);

            dropDown.AddSeparator(setPath + "/");
            return true;
        }

        private static void AddSceneOpen<T>(T dropDown, SceneAsset scene, string setName)
            where T : IDropDown
        {
            var sceneName = scene.name.ToSentence();
            var isOpen = EditorSceneUtil.IsSceneAssetOpen(scene);

            dropDown.AddItem($"{setName}/{sceneName}", isOpen, static data =>
            {
                var scene = (SceneAsset)data;
                var isOpen = EditorSceneUtil.IsSceneAssetOpen(scene);

                if (isOpen)
                {
                    // Can't close if only scene
                    if (SceneManager.sceneCount == 1)
                    {
                        return;
                    }

                    var scenePath = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(scene));

                    if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scenePath }))
                    {
                        return;
                    }

                    EditorSceneManager.CloseScene(scenePath, true);
                }
                else
                {
                    EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene), OpenSceneMode.Additive);
                }
            }, scene);
        }

        private static void AddSceneBake<T>(T dropDown, SceneAsset scene, string setName)
            where T : IDropDown
        {
            var sceneName = scene.name.ToSentence();
            dropDown.AddItem($"{setName}/{sceneName}", false, _ =>
            {
                // If it's already open, we have to close it before converting it
                if (EditorSceneUtil.IsSceneAssetOpen(scene))
                {
                    // Can't close if only scene
                    if (SceneManager.sceneCount == 1)
                    {
                        return;
                    }

                    var scenePath = SceneManager.GetSceneByPath(AssetDatabase.GetAssetPath(scene));

                    if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scenePath }))
                    {
                        return;
                    }

                    EditorSceneManager.CloseScene(scenePath, true);
                }

                if (!EditorSubScenes.TryGetValue(scene, out var subScene) || !subScene)
                {
                    foreach (var subscene in Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None))
                    {
                        if (subscene.SceneAsset == scene)
                        {
                            subScene = subscene;
                            break;
                        }
                    }

                    if (!subScene)
                    {
                        var go = new GameObject { hideFlags = HideFlags.DontSave };
                        subScene = go.AddComponent<SubScene>();
                        subScene.SceneAsset = scene;
                        EditorSubScenes[scene] = subScene;
                    }
                }

                SubSceneUtility.EditScene(subScene);
            }, scene);
        }

        private static void LivingBakingDropdown<T>(T menu)
            where T : IDropDown
        {
            var scenes = new Dictionary<Hash128, SubScene?>();

            var subScenes = Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None);
            foreach (var s in subScenes)
            {
                scenes.Add(s.SceneGUID, s);
            }

            foreach (var world in World.All)
            {
                using var query = world.EntityManager.CreateEntityQuery(typeof(SubSceneLoadData), typeof(SubSceneBuffer));

                var entities = query.ToEntityArray(Allocator.Temp);
                var subSceneLoadDatas = query.ToComponentDataArray<SubSceneLoadData>(Allocator.Temp);

                for (var index = 0; index < entities.Length; index++)
                {
                    var sr = subSceneLoadDatas[index];
                    if (sr.IsRequired)
                    {
                        continue;
                    }

                    var suBSceneBuffer = world.EntityManager.GetBuffer<SubSceneBuffer>(entities[index]);

                    foreach (var ssb in suBSceneBuffer)
                    {
                        scenes.TryAdd(ssb.Scene.Id.GlobalId.AssetGUID, null);
                    }
                }
            }

            foreach (var s in scenes)
            {
                if (s.Value && !TempSubScenes.ContainsKey(s.Key))
                {
                    continue;
                }

                var path = AssetDatabase.GUIDToAssetPath(s.Key);
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (!sceneAsset)
                {
                    continue;
                }

                if (SceneManager.GetSceneByPath(path).isLoaded)
                {
                    menu.AddItem(L10n.Tr(sceneAsset.ToString()), true, static data =>
                    {
                        var (key, subScene) = (KeyValuePair<Hash128, SubScene?>)data;

                        if (TempSubScenes.Remove(key))
                        {
                            if (!subScene)
                            {
                                return;
                            }

                            Object.Destroy(subScene.gameObject);
                        }

                        // Make sure it is still open
                        var scene = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(key));
                        if (!scene.isLoaded)
                        {
                            return;
                        }

                        SceneManager.UnloadSceneAsync(scene);
                    }, s);
                }
                else
                {
                    menu.AddItem(sceneAsset.name, false, static data =>
                    {
                        var (key, subScene) = (KeyValuePair<Hash128, SubScene?>)data;

                        var path = AssetDatabase.GUIDToAssetPath(key);
                        var scene = SceneManager.GetSceneByPath(path);

                        if (scene.isLoaded)
                        {
                            return;
                        }

                        if (!subScene)
                        {
                            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                            subScene = new GameObject().AddComponent<SubScene>();
                            subScene.AutoLoadScene = false;
                            subScene.SceneAsset = sceneAsset;
                            TempSubScenes[subScene.SceneGUID] = subScene;
                        }

                        scene = EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                        scene.isSubScene = true;
                    }, s);
                }
            }

            menu.Finish();
        }
    }
}
#endif