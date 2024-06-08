// <copyright file="EditorMenus.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
#pragma warning disable 0436 // Type 'Log' conflicts with another one in case of InternalVisibleTo

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.ChangeFilterTracking;
    using Unity.Editor.Bridge;
    using Unity.Entities.Editor;
    using Unity.Logging;
    using UnityEditor;
    using UnityEngine;
    using Resources = UnityEngine.Resources;

    public static class EditorMenus
    {
        private const string DebugMenu = "BovineLabs/Debug/";

        private const string LogLevelBLMenu = DebugMenu + "Log Level BovineLabs/";
        private const string DebugLevelVerboseBLMenuEnabled = LogLevelBLMenu + "0. Verbose";
        private const string DebugLevelDebugBLMenuEnabled = LogLevelBLMenu + "1. Debug";
        private const string DebugLevelInfoBLMenuEnabled = LogLevelBLMenu + "2. Info";
        private const string DebugLevelWarningBLMenuEnabled = LogLevelBLMenu + "3. Warning";
        private const string DebugLevelErrorBLMenuEnabled = LogLevelBLMenu + "4. Error";
        private const string DebugLevelFatalBLMenuEnabled = LogLevelBLMenu + "5. Fatal";

        private const string ChangeFilterTracking = DebugMenu + "Change Filter Tracking";

        private const string DataModeMenu = "BovineLabs/DataMode/";
        private const string DataModeDisabled = DataModeMenu + "Automatic";
        private const string DataModeAuthoring = DataModeMenu + "Authoring";
        private const string DataModeMixed = DataModeMenu + "Mixed";
        private const string DataModeRuntime = DataModeMenu + "Runtime";

        private const string DataModeHierarchyMenu = "BovineLabs/DataModeHierarchy/";
        private const string DataModeHierarchyDisabled = DataModeHierarchyMenu + "Automatic";
        private const string DataModeHierarchyAuthoring = DataModeHierarchyMenu + "Authoring";
        private const string DataModeHierarchyMixed = DataModeHierarchyMenu + "Mixed";
        private const string DataModeHierarchyRuntime = DataModeHierarchyMenu + "Runtime";

        private static LogLevel defaultLevel;

        private static LogLevel BLLogLevel
        {
            get
            {
                if (Application.isPlaying)
                {
                    return (LogLevel)BLDebugSystem.LogLevel.Data;
                }

                if (!int.TryParse(EditorPrefs.GetString(BLDebugSystem.LogLevelName, BLDebugSystem.LogLevelDefaultValue.ToString()), out var value))
                {
                    return (LogLevel)BLDebugSystem.LogLevelDefaultValue;
                }

                return (LogLevel)value;
            }

            set
            {
                if (Application.isPlaying)
                {
                    BLDebugSystem.LogLevel.Data = (int)value;
                }
                else
                {
                    EditorPrefs.SetString(BLDebugSystem.LogLevelName, ((int)value).ToString());
                }
            }
        }

        [MenuItem(ChangeFilterTracking, false)]
        private static void ChangeFilterTrackingMenu()
        {
            ChangeFilterTrackingSystem.IsEnabled.Data = !ChangeFilterTrackingSystem.IsEnabled.Data;
        }

        [MenuItem(ChangeFilterTracking, true)]
        private static bool ChangeFilterTrackingValidate()
        {
            Menu.SetChecked(ChangeFilterTracking, ChangeFilterTrackingSystem.IsEnabled.Data);
            return true;
        }

        [MenuItem(DebugLevelVerboseBLMenuEnabled, false)]
        private static void DebugLevelVerboseBLMenu()
        {
            BLLogLevel = LogLevel.Verbose;
        }

        [MenuItem(DebugLevelVerboseBLMenuEnabled, true)]
        private static bool DebugLevelVerboseBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelVerboseBLMenuEnabled, BLLogLevel == LogLevel.Verbose);
            return true;
        }

        [MenuItem(DebugLevelDebugBLMenuEnabled, false)]
        private static void DebugLevelDebugBLMenu()
        {
            BLLogLevel = LogLevel.Debug;
        }

        [MenuItem(DebugLevelDebugBLMenuEnabled, true)]
        private static bool DebugLevelDebugBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelDebugBLMenuEnabled, BLLogLevel == LogLevel.Debug);
            return true;
        }

        [MenuItem(DebugLevelInfoBLMenuEnabled, false)]
        private static void InfoLevelInfoBLMenu()
        {
            BLLogLevel = LogLevel.Info;
        }

        [MenuItem(DebugLevelInfoBLMenuEnabled, true)]
        private static bool InfoLevelInfoBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelInfoBLMenuEnabled, BLLogLevel == LogLevel.Info);
            return true;
        }

        [MenuItem(DebugLevelWarningBLMenuEnabled, false)]
        private static void WarningLevelWarningBLMenu()
        {
            BLLogLevel = LogLevel.Warning;
        }

        [MenuItem(DebugLevelWarningBLMenuEnabled, true)]
        private static bool WarningLevelWarningBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelWarningBLMenuEnabled, BLLogLevel == LogLevel.Warning);
            return true;
        }

        [MenuItem(DebugLevelErrorBLMenuEnabled, false)]
        private static void ErrorLevelErrorBLMenu()
        {
            BLLogLevel = LogLevel.Error;
        }

        [MenuItem(DebugLevelErrorBLMenuEnabled, true)]
        private static bool ErrorLevelErrorBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelErrorBLMenuEnabled, BLLogLevel == LogLevel.Error);
            return true;
        }

        [MenuItem(DebugLevelFatalBLMenuEnabled, false)]
        private static void FatalLevelFatalBLMenu()
        {
            BLLogLevel = LogLevel.Fatal;
        }

        [MenuItem(DebugLevelFatalBLMenuEnabled, true)]
        private static bool FatalLevelFatalBLMenuValidate()
        {
            Menu.SetChecked(DebugLevelFatalBLMenuEnabled, BLLogLevel == LogLevel.Fatal);
            return true;
        }

        [MenuItem(DataModeDisabled, priority = 1)]
        private static void DataModeDisabledMenu()
        {
            SelectionBridge.UpdateSelectionMetaData(Selection.activeContext, DataMode.Disabled);
        }

        [MenuItem(DataModeAuthoring, priority = 2)]
        private static void DataModeAuthoringMenu()
        {
            SelectionBridge.UpdateSelectionMetaData(Selection.activeContext, DataMode.Authoring);
        }

        [MenuItem(DataModeMixed, priority = 3)]
        private static void DataModeMixedMenu()
        {
            SelectionBridge.UpdateSelectionMetaData(Selection.activeContext, DataMode.Mixed);
        }

        [MenuItem(DataModeRuntime, priority = 4)]
        private static void DataModeRuntimeMenu()
        {
            SelectionBridge.UpdateSelectionMetaData(Selection.activeContext, DataMode.Runtime);
        }

        [MenuItem(DataModeHierarchyDisabled, priority = 1)]
        private static void DataModeHierarchyDisabledMenu()
        {
            DataModeHierarchySet(DataMode.Disabled);
        }

        [MenuItem(DataModeHierarchyAuthoring, priority = 2)]
        private static void DataModeHierarchyAuthoringMenu()
        {
            DataModeHierarchySet(DataMode.Authoring);
        }

        [MenuItem(DataModeHierarchyMixed, priority = 3)]
        private static void DataModeHierarchyMixedMenu()
        {
            DataModeHierarchySet(DataMode.Mixed);
        }

        [MenuItem(DataModeHierarchyRuntime, priority = 4)]
        private static void DataModeHierarchyRuntimeMenu()
        {
            DataModeHierarchySet(DataMode.Runtime);
        }

        private static void DataModeHierarchySet(DataMode dataMode)
        {
            foreach (var hierarchyWindow in Resources.FindObjectsOfTypeAll<HierarchyWindow>())
            {
                hierarchyWindow.dataModeController.TryChangeDataMode(dataMode);
            }
        }
    }
}
