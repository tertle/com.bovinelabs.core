// <copyright file="PackageSettingsCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.Utility;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEngine;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;

    /// <summary> Creates settings assets introduced by newly registered or updated packages. </summary>
    internal static class PackageSettingsCreator
    {
        [NoAutoStaticsCleanup]
        private static bool hasPendingChanges;

        [NoAutoStaticsCleanup]
        private static bool isScheduled;

        [NoAutoStaticsCleanup]
        private static bool isProcessing;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            Events.registeredPackages -= OnRegisteredPackages;
            Events.registeredPackages += OnRegisteredPackages;
        }

        private static void OnRegisteredPackages(PackageRegistrationEventArgs args)
        {
            if (args.added.Count == 0 && args.changedTo.Count == 0)
            {
                return;
            }

            hasPendingChanges = true;
            ScheduleCreation();
        }

        private static void ScheduleCreation()
        {
            if (isScheduled)
            {
                return;
            }

            isScheduled = true;
            EditorApplication.delayCall -= CreatePendingSettings;
            EditorApplication.delayCall += CreatePendingSettings;
        }

        private static void CreatePendingSettings()
        {
            EditorApplication.delayCall -= CreatePendingSettings;
            isScheduled = false;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || isProcessing)
            {
                ScheduleCreation();
                return;
            }

            hasPendingChanges = false;
            isProcessing = true;

            try
            {
                CreateAllPackageSettings();
            }
            finally
            {
                isProcessing = false;

                if (hasPendingChanges)
                {
                    ScheduleCreation();
                }
            }
        }

        private static void CreateAllPackageSettings()
        {
            var settingsTypes = ReflectionUtility.GetAllImplementationsRootOnly<ISettings, ScriptableObject>()
                .Where(type => PackageInfo.FindForAssembly(type.Assembly) != null)
                .OrderBy(type => type == typeof(EditorSettings) ? 0 : 1)
                .ThenBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var settingsType in settingsTypes)
            {
                try
                {
                    EditorSettingsUtility.GetSettings(settingsType);
                }
                catch (Exception exception)
                {
                    BLGlobalLogger.LogFatal(new InvalidOperationException($"Failed to create settings asset for {settingsType.FullName}.", exception));
                }
            }
        }
    }
}
