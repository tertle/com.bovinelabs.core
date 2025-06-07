// <copyright file="BLGlobalLogger.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;
    using UnityEngine;

    public static class BLGlobalLogger
    {
        public static LogLevel Level => BLLogger.Level;

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogVerbose(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public static void LogVerboseString(string msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebug(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugLong512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugLong4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public static void LogDebugString(string msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfo(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogInfoString(string msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarning4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogWarningString(string msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogError4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogErrorString(string msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {msg}");
            }
        }

        [HideInCallstack]
        public static void LogFatal(Exception ex)
        {
            if (Level >= LogLevel.Fatal)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        [HideInCallstack]
        public static void LogString(string msg, LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Disabled:
                    break;
                case LogLevel.Fatal:
                case LogLevel.Error:
                    LogErrorString(msg);
                    break;
                case LogLevel.Warning:
                    LogWarningString(msg);
                    break;
                case LogLevel.Info:
                    LogInfoString(msg);
                    break;
                case LogLevel.Debug:
                    LogDebugString(msg);
                    break;
                case LogLevel.Verbose:
                    LogVerboseString(msg);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
