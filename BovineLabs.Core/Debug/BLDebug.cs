// <copyright file="BLDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable CS0436

namespace BovineLabs.Core
{
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Logging;
    using Unity.Logging.Internal;

    [HideInStackTrace]
    public struct BLDebug : IComponentData
    {
        public static readonly BLDebug Default = new() { LoggerHandle = LoggerManager.Logger.Handle };

        internal bool Enabled;
        internal LoggerHandle LoggerHandle;

        public bool IsValid => this.LoggerHandle.IsValid;

        public uint LogID => this.LoggerHandle.Value;

        [Conditional("UNITY_EDITOR")]
        public readonly void Verbose(in FixedString128Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Verbose(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        public readonly void VerboseString(in string msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Verbose(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public readonly void Debug(in FixedString128Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Debug(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public readonly void DebugLong512(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Debug(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public readonly void DebugLong4096(in FixedString4096Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Debug(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public readonly void DebugString(in string msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Debug(msg);
            }
        }

        public readonly void Info(in FixedString128Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Info(msg);
            }
        }

        public readonly void Info512(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Info(msg);
            }
        }

        public readonly void Info(in FixedString4096Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Info(msg);
            }
        }

        public readonly void InfoString(in string msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Info(msg);
            }
        }

        public readonly void Warning(in FixedString128Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Warning(msg);
            }
        }

        public readonly void Warning512(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Warning(msg);
            }
        }

        public readonly void Warning4096(in FixedString4096Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Warning(msg);
            }
        }

        public readonly void WarningString(in string msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Warning(msg);
            }
        }

        public readonly void Error(in FixedString128Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Error(msg);
            }
        }

        public readonly void Error512(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Error(msg);
            }
        }

        public readonly void Error4096(in FixedString4096Bytes msg)
        {
            if (this.Enabled)
            {
                CustomLog.To(this.LoggerHandle).Error(msg);
            }
        }

        public readonly void ErrorString(in string msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Error(msg);
            }
        }
    }
}
