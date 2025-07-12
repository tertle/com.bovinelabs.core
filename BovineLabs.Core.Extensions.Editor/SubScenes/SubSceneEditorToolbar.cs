// <copyright file="SubSceneEditorToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.Editor.Utility;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.SubScenes;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using Hash128 = Unity.Entities.Hash128;
    using Object = UnityEngine.Object;

    [Configurable]
    public static class SubSceneEditorToolbar
    {
        private const int MaxLength = 25;

        [ConfigVar("debug.editor-toolbar.scene-enable", true, "Should the scene baking toolbar buttons be shown. Requires a domain reload to update")]
        private static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<EnabledType>();

        [ConfigVar("debug.editor-toolbar.scene-group", false, "Should scenes be grouped by first character. Useful if you have a lot of scenes")]
        private static readonly SharedStatic<bool> Group = SharedStatic<bool>.GetOrCreate<GroupType>();

        [ConfigVar("debug.editor-toolbar.scene-swap", false, "Swap the left and right mouse functions for the scene button")]
        private static readonly SharedStatic<bool> Swap = SharedStatic<bool>.GetOrCreate<SwapType>();

        private static readonly Dictionary<Hash128, SubScene> TempSubScenes = new();
        private static EditorToolbarButton? startupDropDown;
        private static EditorToolbarButton? sceneDropDown;

        [EditorToolbar(EditorToolbarPosition.RightCenter, -11)]
        public static VisualElement? SceneSelection()
        {
            ConfigVarManager.Init();

            if (!Enabled.Data)
            {
                return null;
            }

            SceneManager.sceneLoaded += (_, _) => CleanupOldSubScenes();
            EditorApplication.playModeStateChanged += SceneOnPlayModeStatusChanged;

            sceneDropDown = new EditorToolbarDropdown { icon = (Texture2D)EditorGUIUtility.IconContent("PreMatCube").image };
            sceneDropDown.AddToClassList("unity-editor-toolbar-element");
            sceneDropDown.clicked += () => SceneSelectionClicked(sceneDropDown.worldBound, Swap.Data);
            sceneDropDown.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    SceneSelectionClicked(sceneDropDown.worldBound, !Swap.Data);
                }
            });
            SceneOnPlayModeStatusChanged(PlayModeStateChange.EnteredEditMode);

            return sceneDropDown;
        }

        [EditorToolbar(EditorToolbarPosition.RightCenter, -10)]
        public static VisualElement? Startup()
        {
            ConfigVarManager.Init();

            if (!Enabled.Data)
            {
                return null;
            }

            startupDropDown = new EditorToolbarDropdown { icon = (Texture2D)EditorGUIUtility.IconContent("PreMatCylinder").image };
            startupDropDown.tooltip = "Overrides the startup set of scenes for quick testing.";
            startupDropDown.AddToClassList("unity-editor-toolbar-element");
            startupDropDown.clicked += () => EditorStartupDropDown(startupDropDown.worldBound);

            EditorApplication.playModeStateChanged += change =>
            {
                switch (change)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                        startupDropDown.SetEnabled(false);
                        break;
                    case PlayModeStateChange.EnteredEditMode:
                        startupDropDown.SetEnabled(true);
                        break;
                }
            };

            UpdateEditModeSelections();

            return startupDropDown;
        }

        private static void SceneOnPlayModeStatusChanged(PlayModeStateChange obj)
        {
            CleanupOldSubScenes();

            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    sceneDropDown!.text = "Live Baking";
                    sceneDropDown.tooltip = "Open a set of scenes as live baking without the need for a SubScene";
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    sceneDropDown!.text = "Scenes";
                    sceneDropDown.tooltip = "Open or close scenes for editing. Left click to open a set of scenes or right click to manually open/close scenes";
                    break;
            }
        }

        private static void UpdateEditModeSelections()
        {
            const string defaultText = "Startup";

            var index = SubSceneEditorSystem.Override.Data;
            if (index < 0)
            {
                startupDropDown!.text = defaultText;
            }
            else
            {
                var sets = EditorSettingsUtility.GetSettings<SubSceneSettings>().EditorSceneSets;
                if (index >= sets.Count || !sets[index])
                {
                    SubSceneEditorSystem.Override.Data = -1;
                    startupDropDown!.text = defaultText;
                }
                else
                {
                    startupDropDown!.text = sets[index].name.ToSentence().Max(MaxLength, "...");
                }
            }
        }

        private static void CleanupOldSubScenes()
        {
            foreach (var s in TempSubScenes.ToArray())
            {
                if (!s.Value)
                {
                    TempSubScenes.Remove(s.Key);
                }
            }
        }

        private static void SceneSelectionClicked(Rect worldBound, bool showScene)
        {
            if (EditorApplication.isPlaying)
            {
                LivingBakingDropdown(worldBound);
            }
            else
            {
                if (showScene)
                {
                    SceneSelectionSceneDropDown(worldBound);
                }
                else
                {
                    SceneSelectionSetDropDown(worldBound);
                }
            }
        }

        private static void SceneSelectionSceneDropDown(Rect worldBound)
        {
            var menu = new GenericMenu();

            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();

            var scenes = settings
                .SceneSets
                .Cast<SubSceneSetBase>()
                .Concat(settings.EditorSceneSets)
                .Where(set => set)
                .SelectMany(set => set.Scenes)
                .Where(scene => scene)
                .Concat(EditorBuildSettings.scenes.Select(s => AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path)))
                .Distinct()
                .OrderBy(s => s.name);

            foreach (var scene in scenes)
            {
                var n = scene.name.ToSentence();
                if (Group.Data)
                {
                    n = $"{n.FirstCharToUpper()[0]}/{n}";
                }

                var isOpen = EditorSceneUtil.IsSceneAssetOpen(scene);

                menu.AddItem(EditorGUIUtility.TrTextContent(n), isOpen, static data =>
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

            menu.DropDown(worldBound);
        }

        private static void SceneSelectionSetDropDown(Rect worldBound)
        {
            var menu = new GenericMenu();

            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();

            var sets = settings.SceneSets.Cast<SubSceneSetBase>().Concat(settings.EditorSceneSets).Where(s => s && s.Scenes.Count > 0).OrderBy(s => s.name);

            foreach (var set in sets)
            {
                var n = set.name.ToSentence();
                if (Group.Data)
                {
                    n = $"{n.FirstCharToUpper()[0]}/{n}";
                }

                menu.AddItem(EditorGUIUtility.TrTextContent(n), false, static data =>
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
            }

            menu.DropDown(worldBound);
        }

        private static void EditorStartupDropDown(Rect worldBound)
        {
            var menu = new GenericMenu();

            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();
            var sets = settings.EditorSceneSets.Select((set, index) => (set, index)).Where(s => s.set).OrderBy(s => s.set.name);

            foreach (var kvp in sets)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(kvp.set.name.ToSentence()), SubSceneEditorSystem.Override.Data == kvp.index, static data =>
                {
                    SubSceneEditorSystem.Override.Data = SubSceneEditorSystem.Override.Data == (int)data ? -1 : (int)data;
                    UpdateEditModeSelections();
                }, kvp.index);
            }

            menu.DropDown(worldBound);
        }

        private static void LivingBakingDropdown(Rect worldBound)
        {
            var menu = new GenericMenu();
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
                    menu.AddItem(EditorGUIUtility.TrTextContent(sceneAsset.ToString()), true, static ss =>
                    {
                        var (key, subScene) = (KeyValuePair<Hash128, SubScene?>)ss;

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
                    menu.AddItem(EditorGUIUtility.TrTextContent(sceneAsset.name), false, static ss =>
                    {
                        var (key, subScene) = (KeyValuePair<Hash128, SubScene?>)ss;

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

                            TempSubScenes[key] = subScene;
                        }

                        scene = EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                        scene.isSubScene = true;
                    }, s);
                }
            }

            menu.DropDown(worldBound);
        }

        private struct EnabledType
        {
        }

        private struct GroupType
        {
        }

        private struct SwapType
        {
        }
    }
}
#endif