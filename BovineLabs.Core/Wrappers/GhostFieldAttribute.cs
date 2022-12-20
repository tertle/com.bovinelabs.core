// <copyright file="GhostFieldAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    public enum SmoothingAction
    {
        Clamp = 0,
        Interpolate = 1 << 0,
        InterpolateAndExtrapolate = 3,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class GhostFieldAttribute : Attribute
    {
        public GhostFieldAttribute()
        {
            this.Quantization = -1;
            this.Smoothing = SmoothingAction.Clamp;
            this.Composite = false;
            this.SubType = 0;
            this.SendData = true;
            this.MaxSmoothingDistance = 0;
        }

        public int Quantization { get; set; }
        public bool Composite { get; set; }
        public SmoothingAction Smoothing { get; set; }
        public int SubType { get; set; }
        public bool SendData { get; set; }

        /// <summary>
        /// The maximum distance between two snapshots for which smoothing will be applied.
        /// If the value changes more than this between two received snapshots the smoothing
        /// action will not be performed.
        /// </summary>
        /// <remarks>
        /// For quaternions the value specified should be sin(theta / 2) - where theta is the maximum angle
        /// you want to apply smoothing for.
        /// </remarks>
        public int MaxSmoothingDistance { get; set; }
    }

    /// <summary>
    /// Add the attribute to prevent a field ICommandData struct to be serialized
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DontSerializeForCommand : Attribute
    {
    }
}
#endif
