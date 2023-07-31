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

    public struct BLDebug : IComponentData
    {
        public static readonly BLDebug Default = new() { LoggerHandle = LoggerManager.Logger.Handle };

        internal bool Enabled;
        internal LoggerHandle LoggerHandle;

        public bool IsValid => this.LoggerHandle.IsValid;

        [Conditional("UNITY_EDITOR")]
        public readonly void Verbose(in FixedString32Bytes msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Verbose(msg);
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("BL_DEBUG")]
        public readonly void Debug(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Debug(msg);
            }
        }

        public readonly void Info(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Info(msg);
            }
        }

        public readonly void Warning(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Warning(msg);
            }
        }

        public readonly void Error(in FixedString512Bytes msg)
        {
            if (this.Enabled)
            {
                Log.To(this.LoggerHandle).Error(msg);
            }
        }
    }
}
