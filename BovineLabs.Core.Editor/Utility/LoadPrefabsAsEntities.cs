// <copyright file="LoadPrefabsAsEntities.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using System.Threading.Tasks;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class LoadPrefabsAsEntities
    {
        private static World? selectedWorld;
        private static bool loading;

        static LoadPrefabsAsEntities()
        {
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool("bl.debug.prefab-loading", false);
            set => EditorPrefs.SetBool("bl.debug.prefab-loading", value);
        }

        private static void OnPostHeaderGUI(Editor editor)
        {
            if (!Enabled)
            {
                return;
            }

            if (editor.target == null)
            {
                return;
            }

            // only display for the Prefab/Model importer not the displayed GameObjects
            if (editor.target is GameObject || editor.target.GetType().Name != "PrefabImporter")
            {
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(editor.target, out var guid, out _))
            {
                return;
            }

            if (loading)
            {
                GUILayout.Label("Loading into world...");
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                var worldValid = selectedWorld is { IsCreated: true };

                if (!worldValid)
                {
                    GUI.enabled = false;
                    selectedWorld = World.DefaultGameObjectInjectionWorld;
                }

                if (GUILayout.Button("Load into world"))
                {
                    var prefabGuid = new GUID(guid);
                    var entity = SceneSystem.LoadSceneAsync(selectedWorld!.Unmanaged, prefabGuid, new SceneSystem.LoadParameters { AutoLoad = true });
                    LoadRemovePrefab(selectedWorld, entity);
                }

                GUI.enabled = true;

                var w = worldValid ? selectedWorld!.Name : "Worlds";

                if (World.All.Count == 0)
                {
                    GUI.enabled = false;
                }

                if (EditorGUILayout.DropdownButton(new GUIContent(w), FocusType.Passive))
                {
                    var menu = new GenericMenu();

                    for (var index = 0; index < World.All.Count; index++)
                    {
                        var world = World.All[index];
                        if ((world.Flags & WorldFlags.Game) != 0)
                        {
                            menu.AddItem(new GUIContent(world.Name), selectedWorld == world, data => selectedWorld = (World)data, world);
                        }
                    }

                    menu.ShowAsContext();
                }

                GUI.enabled = true;
            }
        }

        private static async void LoadRemovePrefab(World world, Entity entity)
        {
            loading = true;
            try
            {
                while (!SceneSystem.IsSceneLoaded(world.Unmanaged, entity))
                {
                    await Task.Delay(1);

                    if (!world.IsCreated)
                    {
                        return;
                    }
                }

                if (!world.EntityManager.HasComponent<PrefabRoot>(entity))
                {
                    return;
                }

                var prefab = world.EntityManager.GetComponentData<PrefabRoot>(entity).Root;

                if (world.EntityManager.HasComponent<Prefab>(prefab))
                {
                    world.EntityManager.RemoveComponent<Prefab>(prefab);
                }

                EntitySelectionProxy.SelectEntity(world, prefab);
            }
            finally
            {
                loading = false;
            }
        }
    }
}
