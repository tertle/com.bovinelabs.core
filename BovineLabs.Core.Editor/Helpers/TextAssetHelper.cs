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

        public static TextAsset CreateForAsset(TextAsset asset, string defaultName, byte[] bytes)
        {
            return !TryGetPath(asset, defaultName, out var path) ? asset : WriteData(path, bytes);
        }

        private static bool TryGetPath(TextAsset data, string defaultName, out string path)
        {
            path = data == null
                ? EditorUtility.SaveFilePanelInProject($"Save {defaultName}", $"{defaultName}.bytes", "bytes", "Save text data to file")
                : AssetDatabase.GetAssetPath(data);

            return !string.IsNullOrWhiteSpace(path);
        }

        private static bool TryGetPath(SerializedProperty serializedProperty, string defaultName, out string path)
        {
            var data = serializedProperty.objectReferenceValue;

            path = data == null
                ? EditorUtility.SaveFilePanelInProject($"Save {defaultName}", $"{defaultName}.bytes", "bytes", "Save text data to file")
                : AssetDatabase.GetAssetPath(data);

            return !string.IsNullOrWhiteSpace(path);
        }

        private static void WriteData(SerializedProperty serializedProperty, string path, byte[] bytes)
        {
            var textAsset = WriteData(path, bytes);

            serializedProperty.objectReferenceValue = textAsset;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        private static TextAsset WriteData(string path, byte[] bytes)
        {
            var dataPath = Application.dataPath;
            dataPath = dataPath.Remove(dataPath.LastIndexOf("Assets", StringComparison.Ordinal), "Assets".Length);
            dataPath += path;

            File.WriteAllBytes(dataPath, bytes);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
    }
}
