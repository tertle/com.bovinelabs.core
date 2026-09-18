#if UNITY_LOCALIZATION
namespace BovineLabs.Core.Editor.Utility
{
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Localization;
    using Unity.Localization.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class LocalizationEditorUtility
    {
        public static IEnumerable<ResourceTableCollection> GetCollections()
        {
            return AssetDatabase.FindAssets("t:ResourceTableCollection")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ResourceTableCollection>);
        }

        public static ResourceTableCollection GetCollection(TableReference reference)
        {
            return reference.ReferenceType switch
            {
                TableReference.Type.Guid => AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(
                    AssetDatabase.GUIDToAssetPath(reference.TableCollectionNameGuid.ToString())),
                TableReference.Type.Name => GetCollections().SingleOrDefault(collection => collection.TableCollectionName == reference.TableCollectionName),
                _ => null,
            };
        }

        public static ResourceTableCollection CreateCollection(string name, string parentFolder)
        {
            var folder = $"{parentFolder}/{name}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parentFolder, name);
            }

            var shared = ScriptableObject.CreateInstance<SharedTableData>();
            shared.TableCollectionName = name;
            AssetDatabase.CreateAsset(shared, $"{folder}/{name} Shared Data.asset");
            var collection = ScriptableObject.CreateInstance<ResourceTableCollection>();
            collection.SharedData = shared;
            AssetDatabase.CreateAsset(collection, $"{folder}/{name}.asset");
            foreach (var locale in LocalizationSettings.Instance.AvailableLocales)
            {
                LocalizationEditorSettings.AddTable(collection, locale);
            }

            return collection;
        }
    }
}
#endif
