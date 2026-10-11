namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEngine;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    internal static class SettingsAssetCache
    {
        private const string SessionKey = "BovineLabs.Core.SettingsAssetCache.v1";

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<string, string[]> Queries = new(StringComparer.Ordinal);

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<string, AssetEntry> Assets = new(StringComparer.Ordinal);

        [NoAutoStaticsCleanup]
        private static readonly HashSet<string> PrefabPaths = new(StringComparer.Ordinal);

        [NoAutoStaticsCleanup]
        private static Type[] settingsTypes;

        [NoAutoStaticsCleanup]
        private static Type[] packageSettingsTypes;

        [NoAutoStaticsCleanup]
        private static string typeSignature;

        [NoAutoStaticsCleanup]
        private static bool restored;

        internal static IReadOnlyList<Type> SettingsTypes
        {
            get
            {
                Restore();
                return settingsTypes;
            }
        }

        internal static IReadOnlyList<Type> PackageSettingsTypes
        {
            get
            {
                Restore();
                return packageSettingsTypes;
            }
        }

        internal static string[] FindAssets(string filter)
        {
            Restore();
            if (Queries.TryGetValue(filter, out var guids))
            {
                // Keep checking identity after reload; an unreported deletion must never become a valid cache hit.
                if (guids.All(guid => !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))))
                {
                    return guids;
                }

                Invalidate();
            }

            guids = AssetDatabase.FindAssets(filter);
            // Import-time searches may be incomplete. Only retain discovery from a settled AssetDatabase.
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                return guids;
            }

            Queries[filter] = guids;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Assets[path] = new AssetEntry
                {
                    Guid = guid,
                    Path = path,
                    TypeName = AssetDatabase.GetMainAssetTypeAtPath(path)?.AssemblyQualifiedName,
                };
            }

            Save();
            return guids;
        }

        internal static void Invalidate()
        {
            Restore();
            Queries.Clear();
            Assets.Clear();
            SessionState.EraseString(SessionKey);
        }

        internal static bool IsTrackedPath(string path)
        {
            Restore();
            return Assets.ContainsKey(path) || PrefabPaths.Contains(path) ||
                   Assets.Keys.Any(assetPath => assetPath.StartsWith(path + "/", StringComparison.Ordinal)) ||
                   PrefabPaths.Any(prefabPath => prefabPath.StartsWith(path + "/", StringComparison.Ordinal));
        }

        internal static bool HasSameAsset(string path, Type type)
        {
            Restore();
            return Assets.TryGetValue(path, out var asset) && asset.TypeName == type?.AssemblyQualifiedName &&
                   asset.Guid == AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets);
        }

        internal static void TrackAuthorings(IEnumerable<SettingsAuthoring> authorings)
        {
            Restore();
            var paths = authorings.Select(AssetDatabase.GetAssetPath).ToArray();
            if (PrefabPaths.SetEquals(paths))
            {
                return;
            }

            PrefabPaths.Clear();
            PrefabPaths.UnionWith(paths);
            Save();
        }

        private static void Restore()
        {
            if (restored)
            {
                return;
            }

            settingsTypes = TypeCache.GetTypesDerivedFrom<ISettings>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters && typeof(ScriptableObject).IsAssignableFrom(type))
                .OrderBy(type => type.AssemblyQualifiedName, StringComparer.Ordinal).ToArray();
            packageSettingsTypes = settingsTypes.Where(type => typeof(SettingsBase).IsAssignableFrom(type) &&
                !settingsTypes.Any(other => other != type && type.IsAssignableFrom(other)) && PackageInfo.FindForAssembly(type.Assembly) != null).ToArray();
            typeSignature = string.Join("\n", settingsTypes.Select(type => type.AssemblyQualifiedName));
            restored = true;

            var json = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            var data = JsonUtility.FromJson<CacheData>(json);
            if (data.TypeSignature != typeSignature)
            {
                SessionState.EraseString(SessionKey);
                return;
            }

            foreach (var query in data.Queries)
            {
                Queries.Add(query.Filter, query.Guids);
            }

            foreach (var asset in data.Assets)
            {
                Assets.Add(asset.Path, asset);
            }

            PrefabPaths.UnionWith(data.PrefabPaths);
        }

        private static void Save()
        {
            var data = new CacheData
            {
                TypeSignature = typeSignature,
                Queries = Queries.Select(pair => new QueryEntry {
                    Filter = pair.Key,
                    Guids = pair.Value
                }).ToArray(),
                Assets = Assets.Values.ToArray(),
                PrefabPaths = PrefabPaths.ToArray(),
            };
            SessionState.SetString(SessionKey, JsonUtility.ToJson(data));
        }

        [Serializable]
        private sealed class CacheData
        {
            public string TypeSignature;
            public QueryEntry[] Queries;
            public AssetEntry[] Assets;
            public string[] PrefabPaths;
        }

        [Serializable]
        private sealed class QueryEntry
        {
            public string Filter;
            public string[] Guids;
        }

        [Serializable]
        private sealed class AssetEntry
        {
            public string Guid;
            public string Path;
            public string TypeName;
        }
    }
}
