// <copyright file="EditorMenus.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using Unity.Logging;
    using UnityEditor;
    using UnityEngine;

    public static class EditorMenus
    {
        public const string DebugMenu = "BovineLabs/Debug/";

        private const int DisableValue = byte.MaxValue;

        private const string LogLevelMenu = DebugMenu + "Log Level Override/";
        private const string DebugLevelDisableMenuEnabled = LogLevelMenu + "Disable";
        private const string DebugLevelVerboseMenuEnabled = LogLevelMenu + "Verbose";
        private const string DebugLevelDebugMenuEnabled = LogLevelMenu + "Debug";
        private const string DebugLevelInfoMenuEnabled = LogLevelMenu + "Info";
        private const string DebugLevelWarningMenuEnabled = LogLevelMenu + "Warning";
        private const string DebugLevelErrorMenuEnabled = LogLevelMenu + "Error";
        private const string DebugLevelFatalMenuEnabled = LogLevelMenu + "Fatal";
        public static readonly string EditorPrefKey = $"BovineLabs_{Application.productName}_";

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            defaultLevel = Log.Logger.MinimalLogLevelAcrossAllSystems;
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

            Log.Logger.SetMinimalLogLevelAcrossAllSinks(logLevel);
            Log.Logger.UpdateMinimalLogLevelAcrossAllSinks();
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
