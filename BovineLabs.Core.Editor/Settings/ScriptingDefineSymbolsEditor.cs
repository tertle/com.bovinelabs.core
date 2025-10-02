// <copyright file="ScriptingDefineSymbolsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.Settings;
    using UnityEditor;
    using UnityEditor.Build;

    [InitializeOnLoad]
    public static class ScriptingDefineSymbolsEditor
    {
        static ScriptingDefineSymbolsEditor()
        {
            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                return;
            }

            var scriptingDefines = settings!.ScriptingDefineSymbols;

            if (scriptingDefines.Count == 0)
            {
                return;
            }

            ApplyDefinesToAll(scriptingDefines, Array.Empty<string>());
        }

        public static void ApplyDefinesToAll(IReadOnlyList<string> scriptingDefines, IReadOnlyList<string> removeDefines)
        {
            foreach (var target in GetInstalledNamedBuildTargets())
            {
                ApplyDefines(target, scriptingDefines, removeDefines);
            }
        }

        private static void ApplyDefines(NamedBuildTarget target, IReadOnlyList<string> addDefines, IReadOnlyList<string> removeDefines)
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

            PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
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
    }
}
