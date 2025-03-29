// <copyright file="BLDebugSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
#define BL_DEBUG_UPDATE
#endif

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Entities;
    using Unity.Logging;
    using Unity.Logging.Internal;
    using Unity.Logging.Sinks;
    using Unity.Mathematics;
    using UnityEngine;

    [Configurable]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.Editor)]
    public partial class BLDebugSystem : SystemBase
    {
        internal const string LogLevelName = "debug.loglevel";
        internal const int LogLevelDefaultValue = (int)Unity.Logging.LogLevel.Error;

#if UNITY_EDITOR || BL_DEBUG
        private const LogLevel MinLogLevel = Unity.Logging.LogLevel.Debug;
#else
        private const LogLevel MinLogLevel = Unity.Logging.LogLevel.Warning;
#endif

        [ConfigVar(LogLevelName, LogLevelDefaultValue, "The log level debugging for BovineLabs libraries.")]
        internal static readonly SharedStatic<int> LogLevel = SharedStatic<int>.GetOrCreate<BLDebugSystem>();

        private LoggerHandle loggerHandle;
        private LogLevel currentLogLevel;

        private static event Action Quitting;

        /// <inheritdoc />
        protected override void OnCreate()
        {
            var netDebugEntity = this.EntityManager.CreateSingleton<BLDebug>();
            this.EntityManager.SetName(netDebugEntity, "DBDebug");

            this.currentLogLevel = ToLogLevel(LogLevel.Data);

            var world = this.World.Name.TrimEnd("World").TrimEnd();

            var managerParameters = LogMemoryManagerParameters.Default;
#if UNITY_EDITOR
            // In the editor we increase the default (64) capacity to allow verbose spamming
            managerParameters.InitialBufferCapacity = 1024 * 512;
#endif

            var loggerConfig = new LoggerConfig().RedirectUnityLogs(false).SyncMode.FatalIsSync();

#if UNITY_EDITOR
            var template = $"{{Level}} | {world} | {{Message}}{{NewLine}}{{Stacktrace}}";
#else
            var template = $"{{Level}} | {world} | {{Message}}";
#endif

            var logger = loggerConfig
#if UNITY_EDITOR
                .CaptureStacktrace()
#endif
                .WriteTo
                .UnityDebugLog(minLevel: this.currentLogLevel, outputTemplate: template)
                .CreateLogger(managerParameters);

            this.loggerHandle = logger.Handle;
            var blDebug = new BLDebug
            {
                LoggerHandle = this.loggerHandle,
                Enabled = true,
            };

            this.EntityManager.SetComponentData(netDebugEntity, blDebug);

#if !BL_DEBUG_UPDATE
            this.Enabled = false;
#endif

            Quitting += this.DisableLogger;
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            var logger = LoggerManager.GetLogger(this.loggerHandle);
            logger?.Dispose();

            Quitting -= this.DisableLogger;
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

        // In 1.0.11 when leaving play mode the log handle gets disposed before the world causing errors in OnDestroy/OnStopRunning that try to use it
        // This is a gross workaround to disable the logger before anything can call it during a shutdown
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            Application.quitting -= OnQuit;
            Quitting?.Invoke();
        }

        private void DisableLogger()
        {
            ref var debug = ref this.EntityManager.GetSingletonRW<BLDebug>().ValueRW;
            debug.Enabled = false;
        }
    }
}
