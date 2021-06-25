// <copyright file="CompilationReport.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
// Based off work from Karl Jones https://gist.github.com/karljj1/9c6cce803096b5cd4511cf0819ff517b

namespace BovineLabs.Basics.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEditor;
    using UnityEditor.Compilation;
    using UnityEngine;

    [InitializeOnLoad]
    internal static class CompilationReport
    {
        private const string AssemblyReloadEventsEditorPref = "AssemblyReloadEventsTime";
        private const string AssemblyCompilationEventsEditorPref = "AssemblyCompilationEvents";
        private static readonly int ScriptAssembliesPathLen = "Library/ScriptAssemblies/".Length;

        private static readonly Dictionary<string, DateTime> StartTimes = new Dictionary<string, DateTime>();
        private static readonly StringBuilder BuildEvents = new StringBuilder();
        private static DateTime compilationTotalTime;

        static CompilationReport()
        {
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.assemblyCompilationStarted += CompilationPipelineOnAssemblyCompilationStarted;
            CompilationPipeline.assemblyCompilationFinished += CompilationPipelineOnAssemblyCompilationFinished;
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEventsOnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEventsOnAfterAssemblyReload;
        }

        private static void OnCompilationStarted(object obj)
        {
            compilationTotalTime = DateTime.UtcNow;
        }

        private static void OnCompilationFinished(object obj)
        {
            var timeSpan = DateTime.UtcNow - compilationTotalTime;
            BuildEvents.AppendFormat("Compilation total: {0:0.00}s\n", timeSpan.TotalMilliseconds / 1000f);
        }

        private static void CompilationPipelineOnAssemblyCompilationStarted(string assembly)
        {
            StartTimes[assembly] = DateTime.UtcNow;
        }

        private static void CompilationPipelineOnAssemblyCompilationFinished(string assembly, CompilerMessage[] arg2)
        {
            var timeSpan = DateTime.UtcNow - StartTimes[assembly];
            var assemblyName = assembly.Substring(ScriptAssembliesPathLen, assembly.Length - ScriptAssembliesPathLen);
            BuildEvents.AppendFormat("{0:0.00}s {1}\n", timeSpan.TotalMilliseconds / 1000f, assemblyName);
        }

        private static void AssemblyReloadEventsOnBeforeAssemblyReload()
        {
            EditorPrefs.SetString(AssemblyReloadEventsEditorPref, DateTime.UtcNow.ToBinary().ToString());
            EditorPrefs.SetString(AssemblyCompilationEventsEditorPref, BuildEvents.ToString());
        }

        private static void AssemblyReloadEventsOnAfterAssemblyReload()
        {
            var binString = EditorPrefs.GetString(AssemblyReloadEventsEditorPref);

            if (long.TryParse(binString, out var bin))
            {
                var date = DateTime.FromBinary(bin);
                var time = DateTime.UtcNow - date;
                var compilationTimes = EditorPrefs.GetString(AssemblyCompilationEventsEditorPref);
                if (!string.IsNullOrEmpty(compilationTimes))
                {
                    Debug.Log("Compilation Report\n" + compilationTimes + "Assembly Reload Time: " + time.TotalSeconds + "s\n");
                }
            }
        }
    }
}