namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using Unity.Profiling;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;

    public static partial class ScriptingDefineSymbolsEditor
    {
        private const string ConfiguredDefinesKey = "BovineLabs.Core.Editor.ScriptingDefineSymbolsEditor.ConfiguredDefines.v1";

        [NoAutoStaticsCleanup]
        private static readonly ProfilerMarker InitializeMarker = new("BovineLabs.Core.Editor.ScriptingDefineSymbolsEditor.Initialize");

        public static void ApplyDefinesToAll(IReadOnlyCollection<string> scriptingDefines, IReadOnlyCollection<string> removeDefines)
        {
            // The inspector and EnsureDefines can both change the settings object before it is saved.
            SessionState.EraseString(ConfiguredDefinesKey);
            ApplyToInstalledTargets(scriptingDefines, removeDefines);
        }

        private static void ApplyToInstalledTargets(IReadOnlyCollection<string> scriptingDefines, IReadOnlyCollection<string> removeDefines)
        {
            foreach (var target in GetInstalledNamedBuildTargets())
            {
                ApplyDefines(target, scriptingDefines, removeDefines);
            }
        }

        [OnCodeInitializing]
        private static void Initialize()
        {
            using var marker = InitializeMarker.Auto();

            var scriptingDefines = GetConfiguredDefines();
            if (scriptingDefines.Length != 0)
            {
                ApplyToInstalledTargets(scriptingDefines, Array.Empty<string>());
            }
        }

        private static string[] GetConfiguredDefines()
        {
            var useCache = !AssetDatabase.IsAssetImportWorkerProcess();
            var revision = useCache ? SettingsAssetChangeTracker.Revision : 0;
            if (useCache)
            {
                var json = SessionState.GetString(ConfiguredDefinesKey, string.Empty);
                if (json.Length != 0)
                {
                    var cached = JsonUtility.FromJson<ConfiguredDefines>(json);
                    if (cached.Revision == revision)
                    {
                        return cached.Symbols;
                    }
                }
            }

            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                return Array.Empty<string>();
            }

            var symbols = settings.ScriptingDefineSymbols.ToArray();
            if (useCache)
            {
                SessionState.SetString(ConfiguredDefinesKey, JsonUtility.ToJson(new ConfiguredDefines
                {
                    Revision = revision,
                    Symbols = symbols,
                }));
            }

            return symbols;
        }

        private static void ApplyDefines(NamedBuildTarget target, IReadOnlyCollection<string> addDefines, IReadOnlyCollection<string> removeDefines)
        {
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);

            var defineList = defines.ToList();

            // Clear any null
            defineList.RemoveAll(string.IsNullOrWhiteSpace);

            foreach (var s in removeDefines)
            {
                defineList.Remove(s);
            }

            foreach (var s in addDefines)
            {
                if (defineList.Contains(s))
                {
                    continue;
                }

                defineList.Add(s);
            }

            if (!defines.SequenceEqual(defineList, StringComparer.Ordinal))
            {
                PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
            }
        }

        private static IEnumerable<NamedBuildTarget> GetInstalledNamedBuildTargets()
        {
            var targets = Enum.GetValues(typeof(BuildTarget))
                .Cast<BuildTarget>()
                .Select(GetNamedBuildTarget)
                .Where(t => t.HasValue)
                .Select(t => t!.Value)
                .ToList();

            if (IsServerBuildTargetInstalled())
            {
                targets.Add(NamedBuildTarget.Server);
            }

            return targets.Distinct();
        }

        private static bool IsServerBuildTargetInstalled()
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Server, out _);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static NamedBuildTarget? GetNamedBuildTarget(BuildTarget buildTarget)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            if (targetGroup == BuildTargetGroup.Unknown)
            {
                return null;
            }

            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, buildTarget))
            {
                return null;
            }

            return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        [Serializable]
        private sealed class ConfiguredDefines
        {
            public int Revision;
            public string[] Symbols;
        }
    }
}
