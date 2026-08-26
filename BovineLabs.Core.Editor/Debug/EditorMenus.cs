// <copyright file="EditorMenus.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable 0436 // Type 'Log' conflicts with another one in case of InternalVisibleTo

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.Utility;
    using UnityEditor;

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

        private const string PrefabLoading = RootMenuTools + "Load Prefabs as Entities";

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

    }
}
