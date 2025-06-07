﻿// <copyright file="EditorMenus.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable 0436 // Type 'Log' conflicts with another one in case of InternalVisibleTo

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.Utility;
    using Unity.Editor.Bridge;
    using Unity.Entities.Editor;
    using UnityEditor;
    using Resources = UnityEngine.Resources;

    public static class EditorMenus
    {
#if BL_TOOLS_MENU
        public const string RootMenu = "Tools/BovineLabs/";
#else
        public const string RootMenu = "BovineLabs/";
#endif
        public const string RootMenuTools = RootMenu + "Tools/";

        private const string LogLevelBLMenu = RootMenu + "Logging/";
        private const string DebugLevelVerboseBLMenuEnabled = LogLevelBLMenu + "6. Verbose";
        private const string DebugLevelDebugBLMenuEnabled = LogLevelBLMenu + "5. Debug";
        private const string DebugLevelInfoBLMenuEnabled = LogLevelBLMenu + "4. Info";
        private const string DebugLevelWarningBLMenuEnabled = LogLevelBLMenu + "3. Warning";
        private const string DebugLevelErrorBLMenuEnabled = LogLevelBLMenu + "2. Error";
        private const string DebugLevelFatalBLMenuEnabled = LogLevelBLMenu + "1. Fatal";

        private const string DataModeSharedMenu = RootMenuTools + "DataMode/";

        private const string DataModeMenu = DataModeSharedMenu + "Inspector/";
        private const string DataModeDisabled = DataModeMenu + "Automatic";
        private const string DataModeAuthoring = DataModeMenu + "Authoring";
        private const string DataModeMixed = DataModeMenu + "Mixed";
        private const string DataModeRuntime = DataModeMenu + "Runtime";

        private const string DataModeHierarchyMenu = DataModeSharedMenu + "Hierarchy/";
        private const string DataModeHierarchyDisabled = DataModeHierarchyMenu + "Automatic";
        private const string DataModeHierarchyAuthoring = DataModeHierarchyMenu + "Authoring";
        private const string DataModeHierarchyMixed = DataModeHierarchyMenu + "Mixed";
        private const string DataModeHierarchyRuntime = DataModeHierarchyMenu + "Runtime";

        private const string PrefabLoading = RootMenuTools + "Load Prefabs as Entities";

        private static LogLevel defaultLevel;

        private static LogLevel BLLogLevel
        {
            get => (LogLevel)BLLogger.CurrentLogLevel.Data;
            set => BLLogger.CurrentLogLevel.Data = (int)value;
        }

        [MenuItem(DebugLevelVerboseBLMenuEnabled, false, priority = -40)]
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

        [MenuItem(DebugLevelDebugBLMenuEnabled, false, priority = -41)]
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

        [MenuItem(DebugLevelInfoBLMenuEnabled, false, priority = -42)]
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

        [MenuItem(DebugLevelWarningBLMenuEnabled, false, priority = -43)]
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

        [MenuItem(DebugLevelErrorBLMenuEnabled, false, priority = -44)]
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

        [MenuItem(DebugLevelFatalBLMenuEnabled, false, priority = -45)]
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

        [MenuItem(PrefabLoading, false, priority = -45)]
        private static void PrefabLoadingMenu()
        {
            LoadPrefabsAsEntities.Enabled = !LoadPrefabsAsEntities.Enabled;
        }

        [MenuItem(PrefabLoading, true)]
        private static bool PrefabLoadingMenuValidate()
        {
            Menu.SetChecked(PrefabLoading, LoadPrefabsAsEntities.Enabled);
            return true;
        }
    }
}
