// <copyright file="EditorMenus.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>
#pragma warning disable 0436 // Type 'Log' conflicts with another one in case of InternalVisibleTo

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.ChangeFilterTracking;
    using Unity.Logging;
    using UnityEditor;
    using UnityEngine;

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
    }
}
