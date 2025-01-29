// <copyright file="CustomLog.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable SA1201
#pragma warning disable SA1202
#pragma warning disable SA1611
#pragma warning disable SA1615

namespace BovineLabs.Core
{
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Logging;
    using Unity.Logging.Internal;
    using Unity.Logging.Internal.Debug;

    [BurstCompile]
    [HideInStackTrace]
    internal static class CustomLog
    {
        internal struct LogContextWithLock
        {
            public LogControllerScopedLock Lock;

            public bool IsValid => this.Lock.IsValid;
        }

        /// <summary> Write to a particular Logger that has the handle. </summary>
        public static LogContextWithLock To(in LoggerHandle handle)
        {
            var @lock = new LogContextWithLock
            {
                Lock = LogControllerScopedLock.Create(handle),
            };

            if (@lock.IsValid)
            {
                PerThreadData.ThreadLoggerHandle = handle;
            }

            return @lock;
        }

        ////////////////////////////////////////////
        /* [Burst] [Verbose] (FixedString128Bytes msg) */

        [BurstCompile]
        private static void WriteBurstedVerbose(in FixedString128Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            var handles = new FixedList512Bytes<PayloadHandle>();

            ref var memManager = ref logController.MemoryManager;

            // Build payloads for each parameter
            var handle = Builder.BuildMessage(msg, ref memManager);
            if (handle.IsValid)
            {
                handles.Add(handle);
            }

            var stackTraceId = logController.NeedsStackTrace ? ManagedStackTraceWrapper.Capture() : 0;

            Builder.BuildDecorators(ref logController, @lock, ref handles);

            // Create disjointed buffer
            handle = memManager.CreateDisjointedPayloadBufferFromExistingPayloads(ref handles);
            if (handle.IsValid)
            {
                // Dispatch message
                logController.DispatchMessage(handle, stackTraceId, LogLevel.Verbose);
            }
            else
            {
                SelfLog.OnFailedToCreateDisjointedBuffer();
                Builder.ForceReleasePayloads(handles, ref memManager);
            }
        }

        public static void Verbose(this LogContextWithLock dec, in FixedString128Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Verbose) == false)
                {
                    return;
                }

                WriteBurstedVerbose(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        ////////////////////////////////////////////

        ////////////////////////////////////////////
        /* [Burst] [Debug] (FixedString4096Bytes msg) */

        [BurstCompile]
        private static void WriteBurstedDebug(in FixedString128Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedDebug<FixedString128Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedDebug(in FixedString512Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedDebug<FixedString512Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedDebug(in FixedString4096Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedDebug<FixedString4096Bytes>(msg, ref logController, ref @lock);
        }

        private static void WriteBurstedDebug<T>(in T msg, ref LogController logController, ref LogControllerScopedLock @lock)
            where T : unmanaged, IUTF8Bytes, INativeList<byte>
        {
            var handles = new FixedList512Bytes<PayloadHandle>();

            ref var memManager = ref logController.MemoryManager;

            // Build payloads for each parameter
            var handle = Builder.BuildMessage(msg, ref memManager);
            if (handle.IsValid)
            {
                handles.Add(handle);
            }

            var stackTraceId = logController.NeedsStackTrace ? ManagedStackTraceWrapper.Capture() : 0;

            Builder.BuildDecorators(ref logController, @lock, ref handles);

            // Create disjointed buffer
            handle = memManager.CreateDisjointedPayloadBufferFromExistingPayloads(ref handles);
            if (handle.IsValid)
            {
                // Dispatch message
                logController.DispatchMessage(handle, stackTraceId, LogLevel.Debug);
            }
            else
            {
                SelfLog.OnFailedToCreateDisjointedBuffer();
                Builder.ForceReleasePayloads(handles, ref memManager);
            }
        }

        public static void Debug(this LogContextWithLock dec, in FixedString128Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Debug) == false)
                {
                    return;
                }

                WriteBurstedDebug(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Debug(this LogContextWithLock dec, in FixedString512Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Debug) == false)
                {
                    return;
                }

                WriteBurstedDebug(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Debug(this LogContextWithLock dec, in FixedString4096Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Debug) == false)
                {
                    return;
                }

                WriteBurstedDebug(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        ////////////////////////////////////////////

        ////////////////////////////////////////////
        /* [Burst] [Info] (FixedString4096Bytes msg) */

        [BurstCompile]
        private static void WriteBurstedInfo(in FixedString128Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedInfo<FixedString128Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedInfo(in FixedString512Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedInfo<FixedString512Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedInfo(in FixedString4096Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedInfo<FixedString4096Bytes>(msg, ref logController, ref @lock);
        }

        private static void WriteBurstedInfo<T>(in T msg, ref LogController logController, ref LogControllerScopedLock @lock)
            where T : unmanaged, IUTF8Bytes, INativeList<byte>
        {
            var handles = new FixedList512Bytes<PayloadHandle>();

            ref var memManager = ref logController.MemoryManager;

            // Build payloads for each parameter
            var handle = Builder.BuildMessage(msg, ref memManager);
            if (handle.IsValid)
            {
                handles.Add(handle);
            }

            var stackTraceId = logController.NeedsStackTrace ? ManagedStackTraceWrapper.Capture() : 0;

            Builder.BuildDecorators(ref logController, @lock, ref handles);

            // Create disjointed buffer
            handle = memManager.CreateDisjointedPayloadBufferFromExistingPayloads(ref handles);
            if (handle.IsValid)
            {
                // Dispatch message
                logController.DispatchMessage(handle, stackTraceId, LogLevel.Info);
            }
            else
            {
                SelfLog.OnFailedToCreateDisjointedBuffer();
                Builder.ForceReleasePayloads(handles, ref memManager);
            }
        }

        public static void Info(this LogContextWithLock dec, in FixedString128Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Info) == false)
                {
                    return;
                }

                WriteBurstedInfo(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Info(this LogContextWithLock dec, in FixedString512Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Info) == false)
                {
                    return;
                }

                WriteBurstedInfo(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Info(this LogContextWithLock dec, in FixedString4096Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Info) == false)
                {
                    return;
                }

                WriteBurstedInfo(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        ////////////////////////////////////////////

        ////////////////////////////////////////////
        /* [Burst] [Warning] (FixedString4096Bytes msg) */

        [BurstCompile]
        private static void WriteBurstedWarning(in FixedString128Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedWarning<FixedString128Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedWarning(in FixedString512Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedWarning<FixedString512Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedWarning(in FixedString4096Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedWarning<FixedString4096Bytes>(msg, ref logController, ref @lock);
        }

        private static void WriteBurstedWarning<T>(in T msg, ref LogController logController, ref LogControllerScopedLock @lock)
            where T : unmanaged, IUTF8Bytes, INativeList<byte>
        {
            var handles = new FixedList512Bytes<PayloadHandle>();

            ref var memManager = ref logController.MemoryManager;

            // Build payloads for each parameter
            var handle = Builder.BuildMessage(msg, ref memManager);
            if (handle.IsValid)
            {
                handles.Add(handle);
            }

            var stackTraceId = logController.NeedsStackTrace ? ManagedStackTraceWrapper.Capture() : 0;

            Builder.BuildDecorators(ref logController, @lock, ref handles);

            // Create disjointed buffer
            handle = memManager.CreateDisjointedPayloadBufferFromExistingPayloads(ref handles);
            if (handle.IsValid)
            {
                // Dispatch message
                logController.DispatchMessage(handle, stackTraceId, LogLevel.Warning);
            }
            else
            {
                SelfLog.OnFailedToCreateDisjointedBuffer();
                Builder.ForceReleasePayloads(handles, ref memManager);
            }
        }

        public static void Warning(this LogContextWithLock dec, in FixedString128Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Warning) == false)
                {
                    return;
                }

                WriteBurstedWarning(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Warning(this LogContextWithLock dec, in FixedString512Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Warning) == false)
                {
                    return;
                }

                WriteBurstedWarning(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Warning(this LogContextWithLock dec, in FixedString4096Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Warning) == false)
                {
                    return;
                }

                WriteBurstedWarning(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        ////////////////////////////////////////////

        ////////////////////////////////////////////
        /* [Burst] [Error] (FixedString4096Bytes msg) */

        [BurstCompile]
        private static void WriteBurstedError(in FixedString128Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedError<FixedString128Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedError(in FixedString512Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedError<FixedString512Bytes>(msg, ref logController, ref @lock);
        }

        [BurstCompile]
        private static void WriteBurstedError(in FixedString4096Bytes msg, ref LogController logController, ref LogControllerScopedLock @lock)
        {
            WriteBurstedError<FixedString4096Bytes>(msg, ref logController, ref @lock);
        }

        private static void WriteBurstedError<T>(in T msg, ref LogController logController, ref LogControllerScopedLock @lock)
            where T : unmanaged, IUTF8Bytes, INativeList<byte>
        {
            var handles = new FixedList512Bytes<PayloadHandle>();

            ref var memManager = ref logController.MemoryManager;

            // Build payloads for each parameter
            var handle = Builder.BuildMessage(msg, ref memManager);
            if (handle.IsValid)
            {
                handles.Add(handle);
            }

            var stackTraceId = logController.NeedsStackTrace ? ManagedStackTraceWrapper.Capture() : 0;

            Builder.BuildDecorators(ref logController, @lock, ref handles);

            // Create disjointed buffer
            handle = memManager.CreateDisjointedPayloadBufferFromExistingPayloads(ref handles);
            if (handle.IsValid)
            {
                // Dispatch message
                logController.DispatchMessage(handle, stackTraceId, LogLevel.Error);
            }
            else
            {
                SelfLog.OnFailedToCreateDisjointedBuffer();
                Builder.ForceReleasePayloads(handles, ref memManager);
            }
        }

        public static void Error(this LogContextWithLock dec, in FixedString128Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Error) == false)
                {
                    return;
                }

                WriteBurstedError(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Error(this LogContextWithLock dec, in FixedString512Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Error) == false)
                {
                    return;
                }

                WriteBurstedError(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        public static void Error(this LogContextWithLock dec, in FixedString4096Bytes msg)
        {
            try
            {
                if (dec.Lock.IsValid == false)
                {
                    return;
                }

                ref var logController = ref dec.Lock.GetLogController();
                if (logController.HasSinksFor(LogLevel.Error) == false)
                {
                    return;
                }

                WriteBurstedError(msg, ref logController, ref dec.Lock);
            }
            finally
            {
                dec.Lock.Dispose();
                PerThreadData.ThreadLoggerHandle = default;
            }
        }

        ////////////////////////////////////////////
    }
}
