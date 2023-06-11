// <copyright file="BLDebugSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define BL_DEBUG_UPDATE
#endif

namespace BovineLabs.Core
{
    using System.IO;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Logging;
    using Unity.Logging.Internal;
    using Unity.Logging.Sinks;
    using Unity.Mathematics;
    using UnityEngine;

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class BLDebugSystem : SystemBase
    {
        internal const string LogLevelName = "debug.loglevel";
        internal const string LogLevelDesc = "The log level debugging for bovinelabs libraries.";
        internal const int LogLevelDefaultValue = (int)Unity.Logging.LogLevel.Error;

        [ConfigVar(LogLevelName, LogLevelDefaultValue, LogLevelDesc)]
        internal static readonly SharedStatic<int> LogLevel = SharedStatic<int>.GetOrCreate<BLDebugSystem>();

#if UNITY_EDITOR || BL_DEBUG
        private const LogLevel MinLogLevel = Unity.Logging.LogLevel.Debug;
#else
        private const LogLevel MinLogLevel = Unity.Logging.LogLevel.Warning;
#endif

        private LoggerHandle loggerHandle;
        private LogLevel currentLogLevel;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            var netDebugEntity = this.EntityManager.CreateEntity(ComponentType.ReadWrite<BLDebug>());
            this.EntityManager.SetName(netDebugEntity, "DBDebug");

            this.currentLogLevel = ToLogLevel(LogLevel.Data);
            var logDir = GetCurrentAbsoluteLogDirectory();

            var world = this.World.Name.TrimEnd("World").TrimEnd();

            var managerParameters = LogMemoryManagerParameters.Default;
#if UNITY_EDITOR
            // In editor we increase the default (64) capacity to allow verbose spamming
            managerParameters.InitialBufferCapacity = 1024 * 512;
#endif

            var logger = new LoggerConfig()
                .SyncMode.FatalIsSync()
                .WriteTo.JsonFile(
                    Path.Combine(logDir, "Output.log.json"),
                    minLevel: MinLogLevel,
                    outputTemplate: $"[{{Timestamp}}] {{Level}} | {world} | {{Message}}")
                .WriteTo.UnityDebugLog(
                    minLevel: this.currentLogLevel,
                    outputTemplate: $"{{Level}} | {world} | {{Message}}")
                .CreateLogger(managerParameters);

            this.loggerHandle = logger.Handle;
            var blDebug = new BLDebug { LoggerHandle = this.loggerHandle };
            this.EntityManager.SetComponentData(netDebugEntity, blDebug);

#if !BL_DEBUG_UPDATE
            this.Enabled = false;
#endif
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            this.Dependency.Complete();
            ref var debug = ref SystemAPI.GetSingletonRW<BLDebug>().ValueRW;

            var logger = LoggerManager.GetLogger(debug.LoggerHandle);
            logger?.Dispose();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if BL_DEBUG_UPDATE
            if ((int)this.currentLogLevel != LogLevel.Data)
            {
                this.currentLogLevel = ToLogLevel(LogLevel.Data);

                var logger = LoggerManager.GetLogger(this.loggerHandle);
                logger.GetSink<UnityDebugLogSink>().SetMinimalLogLevel(this.currentLogLevel);
            }
#endif
        }

        private static LogLevel ToLogLevel(int level)
        {
            return (LogLevel)math.clamp(level, 0, (int)Unity.Logging.LogLevel.Fatal);
        }

        /// <summary> <see cref="Unity.Logging.DefaultSettings.GetCurrentAbsoluteLogDirectory" />. </summary>
        private static string GetCurrentAbsoluteLogDirectory()
        {
#if UNITY_DOTSRUNTIME
            var args = Environment.GetCommandLineArgs();
            var optIndex = System.Array.IndexOf(args, "-logFile");
            if (optIndex >=0 && ++optIndex < (args.Length - 1) && !args[optIndex].StartsWith("-"))
                return args[optIndex];

            var dir = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            if (string.IsNullOrEmpty(dir))
            {
                dir = Environment.GetCommandLineArgs()[0];
            }
            if (string.IsNullOrEmpty(dir))
            {
                dir = Directory.GetCurrentDirectory();
            }
            dir = Path.Combine(Path.GetDirectoryName(dir) ?? "", "Logs");
            Directory.CreateDirectory(dir);
            return dir;
#elif UNITY_EDITOR
            var projectFolder = Path.GetDirectoryName(Application.dataPath);
            var dir = Path.Combine(projectFolder!, "Logs");
            Directory.CreateDirectory(dir);
            return dir;
#else
            var logPath = Application.consoleLogPath;
            if (string.IsNullOrEmpty(logPath) == false)
            {
                 return Path.GetDirectoryName(logPath);
            }

            var dir = Path.Combine(Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(dir);
            return dir;
#endif
        }
    }
}
