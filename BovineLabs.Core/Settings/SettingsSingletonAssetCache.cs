#if UNITY_EDITOR
namespace BovineLabs.Core.Settings
{
    using System;
    using System.Linq;
    using Unity.Profiling;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;

    internal static partial class SettingsSingletonAssetCache
    {
        private const string CacheKey = "BovineLabs.Core.SettingsSingletonAssetCache.v2";

        [NoAutoStaticsCleanup]
        private static readonly ProfilerMarker SearchMarker = new("BovineLabs.Core.SettingsSingleton.SearchAssets");

        internal static string[] FindGuids()
        {
            // Import workers have their own asset database view and don't own the editor session cache.
            if (AssetDatabase.IsAssetImportWorkerProcess())
            {
                return SearchAssets();
            }

            var revision = SettingsAssetChangeTracker.Revision;
            var typeSignature = string.Join("\n", TypeCache.GetTypesDerivedFrom<SettingsSingleton>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .Select(type => type.AssemblyQualifiedName)
                .OrderBy(name => name, StringComparer.Ordinal));

            var json = SessionState.GetString(CacheKey, string.Empty);
            if (json.Length != 0)
            {
                var cached = JsonUtility.FromJson<CacheData>(json);
                if (cached.Revision == revision && cached.TypeSignature == typeSignature &&
                    cached.Guids.All(guid => AssetDatabase.GUIDToAssetPath(guid).Length != 0))
                {
                    return cached.Guids;
                }

                SessionState.EraseString(CacheKey);
            }

            var guids = SearchAssets();
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                // A search during refresh can see an incomplete database. Populate the cache after it settles.
                EditorApplication.delayCall -= PopulateWhenReady;
                EditorApplication.delayCall += PopulateWhenReady;
            }
            else
            {
                SessionState.SetString(CacheKey, JsonUtility.ToJson(new CacheData
                {
                    Revision = revision,
                    TypeSignature = typeSignature,
                    Guids = guids,
                }));
            }

            return guids;
        }

        private static string[] SearchAssets()
        {
            using var marker = SearchMarker.Auto();
            return AssetDatabase.FindAssets("t:SettingsSingleton a:all");
        }

        private static void PopulateWhenReady()
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += PopulateWhenReady;
                return;
            }

            FindGuids();
        }

        [OnCodeUnloading]
        private static void CancelPopulate()
        {
            EditorApplication.delayCall -= PopulateWhenReady;
        }

        [Serializable]
        private sealed class CacheData
        {
            public int Revision;
            public string TypeSignature;
            public string[] Guids;
        }
    }
}
#endif
