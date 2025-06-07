// <copyright file="ObjectDefinitionMenu.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System.IO;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class ObjectDefinitionMenu
    {
        private const string DefaultDirectory = "Assets/Settings/Definitions";
        private const string ReplaceSceneDefinitionsWithInstantiateName = EditorMenus.RootMenuTools + "Replace Scene Definitions with Instantiate";

        [MenuItem(EditorMenus.RootMenuTools + "Create Definitions from Assets", priority = -17)]
        public static void CreateDefinitionsFromAssets()
        {
            if (Selection.gameObjects == null)
            {
                return;
            }

            var directory = EditorSettingsUtility.GetAssetDirectory("definitions", DefaultDirectory);

            foreach (var select in Selection.gameObjects)
            {
                var path = Path.Combine(directory, $"{select.name}Definition.asset");
                var definition = ScriptableObject.CreateInstance<ObjectDefinition>();

                definition.Prefab = select;
                ObjectDefinitionInspector.AddAuthoring(select, definition);

                AssetDatabase.CreateAsset(definition, path);
                AssetDatabase.ImportAsset(path);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem(ReplaceSceneDefinitionsWithInstantiateName, priority = -16)]
        public static void ReplaceSceneDefinitionsWithInstantiate()
        {
            var scene = SceneManager.GetActiveScene();

            // Use roots instead of Object.Find to keep order
            foreach (var go in scene.GetRootGameObjects())
            {
                TryReplace(go);
            }

            return;

            static void TryReplace(GameObject go)
            {
                if (go.GetComponent<ObjectDefinitionAuthoring>() != null)
                {
                    ObjectInstantiate.TryReplace(go);
                }
                else
                {
                    foreach (Transform c in go.transform)
                    {
                        TryReplace(c.gameObject);
                    }
                }
            }
        }

        [MenuItem(ReplaceSceneDefinitionsWithInstantiateName, true)]
        public static bool ReplaceSceneDefinitionsWithInstantiateValidate()
        {
            return !EditorApplication.isPlaying;
        }
    }
}
#endif
