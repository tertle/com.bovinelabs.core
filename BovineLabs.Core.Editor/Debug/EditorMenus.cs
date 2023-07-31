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

        private const int DisableValue = byte.MaxValue;

        private const string LogLevelMenu = DebugMenu + "Log Level Override/";
        private const string DebugLevelDisableMenuEnabled = LogLevelMenu + "Disable";
        private const string DebugLevelVerboseMenuEnabled = LogLevelMenu + "Verbose";
        private const string DebugLevelDebugMenuEnabled = LogLevelMenu + "Debug";
        private const string DebugLevelInfoMenuEnabled = LogLevelMenu + "Info";
        private const string DebugLevelWarningMenuEnabled = LogLevelMenu + "Warning";
        private const string DebugLevelErrorMenuEnabled = LogLevelMenu + "Error";
        private const string DebugLevelFatalMenuEnabled = LogLevelMenu + "Fatal";

        private const string LogLevelBLMenu = DebugMenu + "Log Level BovineLabs/";
        private const string DebugLevelVerboseBLMenuEnabled = LogLevelBLMenu + "Verbose";
        private const string DebugLevelDebugBLMenuEnabled = LogLevelBLMenu + "Debug";
        private const string DebugLevelInfoBLMenuEnabled = LogLevelBLMenu + "Info";
        private const string DebugLevelWarningBLMenuEnabled = LogLevelBLMenu + "Warning";
        private const string DebugLevelErrorBLMenuEnabled = LogLevelBLMenu + "Error";
        private const string DebugLevelFatalBLMenuEnabled = LogLevelBLMenu + "Fatal";

        private const string ChangeFilterTracking = DebugMenu + "Change Filter Tracking";

        private static readonly string EditorPrefKey = $"BovineLabs_{Application.productName}_";
        private static readonly string LogLevelKey = EditorPrefKey + "LogLevel";
        private static LogLevel defaultLevel;

        private static LogLevel LogLevel
        {
            get => (LogLevel)EditorPrefs.GetInt(LogLevelKey, DisableValue);
            set
            {
                EditorPrefs.SetInt(LogLevelKey, (int)value);
                UpdateLogLevel(value);
            }
        }

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            defaultLevel = Unity.Logging.Log.Logger.MinimalLogLevelAcrossAllSystems;
            UpdateLogLevel(LogLevel);
        }

        private static void UpdateLogLevel(LogLevel logLevel)
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (logLevel == (LogLevel)DisableValue)
            {
                logLevel = defaultLevel;
            }

            Unity.Logging.Log.Logger.SetMinimalLogLevelAcrossAllSinks(logLevel);
            Unity.Logging.Log.Logger.UpdateMinimalLogLevelAcrossAllSinks();
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

        [MenuItem(DebugLevelDisableMenuEnabled, false)]
        private static void DebugLevelDisableMenu()
        {
            LogLevel = (LogLevel)DisableValue;
        }

        [MenuItem(DebugLevelDisableMenuEnabled, true)]
        private static bool DebugLevelDisableMenuValidate()
        {
            Menu.SetChecked(DebugLevelDisableMenuEnabled, LogLevel == (LogLevel)DisableValue);
            return true;
        }

        [MenuItem(DebugLevelVerboseMenuEnabled, false)]
        private static void DebugLevelVerboseMenu()
        {
            LogLevel = LogLevel.Verbose;
        }

        [MenuItem(DebugLevelVerboseMenuEnabled, true)]
        private static bool DebugLevelVerboseMenuValidate()
        {
            Menu.SetChecked(DebugLevelVerboseMenuEnabled, LogLevel == LogLevel.Verbose);
            return true;
        }

        [MenuItem(DebugLevelDebugMenuEnabled, false)]
        private static void DebugLevelDebugMenu()
        {
            LogLevel = LogLevel.Debug;
        }

        [MenuItem(DebugLevelDebugMenuEnabled, true)]
        private static bool DebugLevelDebugMenuValidate()
        {
            Menu.SetChecked(DebugLevelDebugMenuEnabled, LogLevel == LogLevel.Debug);
            return true;
        }

        [MenuItem(DebugLevelInfoMenuEnabled, false)]
        private static void InfoLevelInfoMenu()
        {
            LogLevel = LogLevel.Info;
        }

        [MenuItem(DebugLevelInfoMenuEnabled, true)]
        private static bool InfoLevelInfoMenuValidate()
        {
            Menu.SetChecked(DebugLevelInfoMenuEnabled, LogLevel == LogLevel.Info);
            return true;
        }

        [MenuItem(DebugLevelWarningMenuEnabled, false)]
        private static void WarningLevelWarningMenu()
        {
            LogLevel = LogLevel.Warning;
        }

        [MenuItem(DebugLevelWarningMenuEnabled, true)]
        private static bool WarningLevelWarningMenuValidate()
        {
            Menu.SetChecked(DebugLevelWarningMenuEnabled, LogLevel == LogLevel.Warning);
            return true;
        }

        [MenuItem(DebugLevelErrorMenuEnabled, false)]
        private static void ErrorLevelErrorMenu()
        {
            LogLevel = LogLevel.Error;
        }

        [MenuItem(DebugLevelErrorMenuEnabled, true)]
        private static bool ErrorLevelErrorMenuValidate()
        {
            Menu.SetChecked(DebugLevelErrorMenuEnabled, LogLevel == LogLevel.Error);
            return true;
        }

        [MenuItem(DebugLevelFatalMenuEnabled, false)]
        private static void FatalLevelFatalMenu()
        {
            LogLevel = LogLevel.Fatal;
        }

        [MenuItem(DebugLevelFatalMenuEnabled, true)]
        private static bool FatalLevelFatalMenuValidate()
        {
            Menu.SetChecked(DebugLevelFatalMenuEnabled, LogLevel == LogLevel.Fatal);
            return true;
        }
    }
}
