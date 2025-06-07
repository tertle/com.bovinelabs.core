// <copyright file="BLDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable CS0436

namespace BovineLabs.Core
{
    using System.Diagnostics;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;

    [Configurable]
    public struct BLLogger : IComponentData
    {
        internal const string LogLevelName = "debug.loglevel";
        internal const int LogLevelDefaultValue = (int)LogLevel.Warning;

        [ConfigVar(LogLevelName, LogLevelDefaultValue, "The log level debugging for BovineLabs libraries.")]
        internal static readonly SharedStatic<int> CurrentLogLevel = SharedStatic<int>.GetOrCreate<BLLogger>();

        public static LogLevel Level => (LogLevel)CurrentLogLevel.Data;

        public bool IsValid => !this.World.IsEmpty;

        internal FixedString32Bytes World;

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public readonly void LogVerbose(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [HideInCallstack]
        public readonly void LogVerboseString(string msg)
        {
            if (Level >= LogLevel.Verbose)
            {
                UnityEngine.Debug.Log($"V | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void LogDebug(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void LogDebug512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void LogDebug4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        [HideInCallstack]
        public readonly void LogDebugString(string msg)
        {
            if (Level >= LogLevel.Debug)
            {
                UnityEngine.Debug.Log($"Debug | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogInfo(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogInfo512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogInfo4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogInfoString(string msg)
        {
            if (Level >= LogLevel.Info)
            {
                UnityEngine.Debug.Log($"Info  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogWarning(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogWarning512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogWarning4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogWarningString(string msg)
        {
            if (Level >= LogLevel.Warning)
            {
                UnityEngine.Debug.LogWarning($"Warn  | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogError(in FixedString128Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogError512(in FixedString512Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogError4096(in FixedString4096Bytes msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }

        [HideInCallstack]
        public readonly void LogErrorString(string msg)
        {
            if (Level >= LogLevel.Error)
            {
                UnityEngine.Debug.LogError($"Error | {this.World} | {msg}");
            }
        }
    }
}
