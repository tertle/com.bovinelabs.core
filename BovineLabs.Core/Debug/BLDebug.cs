// <copyright file="BLDebug.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Logging;

    public struct BLDebug : IComponentData
    {
        internal LoggerHandle LoggerHandle;

        [Conditional("UNITY_EDITOR")]
        public void Verbose(in FixedString32Bytes msg)
        {
            Unity.Logging.Log.To(this.LoggerHandle).Verbose(msg);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public void Debug(in FixedString512Bytes msg)
        {
            Unity.Logging.Log.To(this.LoggerHandle).Debug(msg);
        }

        public void Info(in FixedString512Bytes msg)
        {
            Unity.Logging.Log.To(this.LoggerHandle).Info(msg);
        }

        public void Warning(in FixedString512Bytes msg)
        {
            Unity.Logging.Log.To(this.LoggerHandle).Warning(msg);
        }

        public void Error(in FixedString512Bytes msg)
        {
            Unity.Logging.Log.To(this.LoggerHandle).Error(msg);
        }
    }
}
