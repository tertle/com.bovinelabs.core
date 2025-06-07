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
        [ConfigVar("debug.livebaking-toolbar-button", true, "Should the live baking toolbar button be shown. Requires a domain reload to update")]
        private static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<EnabledType>();

        private static readonly Dictionary<Hash128, SubScene> TempSubScenes = new();
        private static EditorToolbarButton? dropDown;

        [EditorToolbar(EditorToolbarPosition.RightCenter, -10)]
        public static VisualElement? LiveBaking()
        {
            ConfigVarManager.Init();

            if (!Enabled.Data)
            {
                return null;
            }

            SceneManager.sceneLoaded += (_, _) => CleanupOldSubScenes();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            dropDown = new EditorToolbarDropdown { icon = (Texture2D)EditorGUIUtility.IconContent("d_PreMatCube").image };
            dropDown.AddToClassList("unity-editor-toolbar-element");
            dropDown.clicked += () => ClickEvent(dropDown.worldBound);
            UpdateScenariosText();

            return dropDown;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            CleanupOldSubScenes();

            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    dropDown!.text = "Live Baking";
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    UpdateScenariosText();
                    break;
            }
        }

        private static void UpdateScenariosText()
        {
            const string defaultText = "Startup";

            var index = SubSceneEditorSystem.Override.Data;
            if (index < 0)
            {
                dropDown!.text = defaultText;
            }
            else
            {
                var sets = EditorSettingsUtility.GetSettings<SubSceneSettings>().EditorSceneSets;
                if (index >= sets.Count || !sets[index])
                {
                    SubSceneEditorSystem.Override.Data = -1;
                    dropDown!.text = defaultText;
                }
                else
                {
                    dropDown!.text = sets[index].name;
                }
            }
        }

        private static void CleanupOldSubScenes()
        {
            foreach (var s in TempSubScenes.ToArray())
            {
                if (s.Value == null)
                {
                    TempSubScenes.Remove(s.Key);
                }
            }
        }

        private static void ClickEvent(Rect worldBound)
        {
            if (!EditorApplication.isPlaying)
            {
                SubSceneSetDropdown(worldBound);
            }
            else
            {
                LivingBakingDropdown(worldBound);
            }
        }

        private static void SubSceneSetDropdown(Rect worldBound)
        {
            var menu = new GenericMenu();

            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();

            for (var index = 0; index < settings.EditorSceneSets.Count; index++)
            {
                var set = settings.EditorSceneSets[index];
                if (!set)
                {
                    continue;
                }

                menu.AddItem(EditorGUIUtility.TrTextContent(set.name), SubSceneEditorSystem.Override.Data == index, static data =>
                {
                    SubSceneEditorSystem.Override.Data = SubSceneEditorSystem.Override.Data == (int)data ? -1 : (int)data;
                    UpdateScenariosText();
                }, index);
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

                if (sceneAsset == null)
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
                            if (subScene == null)
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

                        if (subScene == null)
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
    }
}
#endif
