// <copyright file="TextAssetHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Helpers
{
    using System;
    using System.IO;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine;

    public static class TextAssetHelper
    {
        public static void CreateForProperty(SerializedProperty serializedProperty, string defaultName, NativeArray<byte> bytes)
        {
            if (!TryGetPath(serializedProperty, defaultName, out var path))
            {
                return;
            }

            WriteData(serializedProperty, path, bytes.ToArray());
        }

        public static void CreateForProperty(SerializedProperty serializedProperty, string defaultName, byte[] bytes)
        {
            if (!TryGetPath(serializedProperty, defaultName, out var path))
            {
                return;
            }

            WriteData(serializedProperty, path, bytes);
        }

        private static bool TryGetPath(SerializedProperty serializedProperty, string defaultName, out string path)
        {
            var data = serializedProperty.objectReferenceValue;

            if (data == null)
            {
                path = EditorUtility.SaveFilePanel($"Save {defaultName}", string.Empty, $"{defaultName}.bytes", "bytes");
            }
            else
            {
                path = Application.dataPath;
                path = path.Remove(path.LastIndexOf("Assets", StringComparison.Ordinal), "Assets".Length);
                path += AssetDatabase.GetAssetPath(data);
            }

            return !string.IsNullOrWhiteSpace(path);
        }

        private static void WriteData(SerializedProperty serializedProperty, string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            var assetPath = path.Remove(0, Application.dataPath.Length - "Assets".Length);

            serializedProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
