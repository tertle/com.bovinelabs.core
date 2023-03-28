// <copyright file="ObjectDefinitionMenu.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.AssetManagement
{
    using System.IO;
    using BovineLabs.Core.AssetManagement;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEngine;

    public static class ObjectDefinitionMenu
    {
        private const string DefaultDirectory = "Assets/Configs/Definitions";

        [MenuItem("BovineLabs/Utility/Create Definitions From Assets")]
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
                AssetDatabase.CreateAsset(definition, path);
                AssetDatabase.ImportAsset(path);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
