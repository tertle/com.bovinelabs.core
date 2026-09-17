#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using System;

    public enum SmoothingAction
    {
        Clamp = 0,

        Interpolate = 1 << 0,

        /// <summary>
        /// Extrapolation is capped by ClientTickRate.MaxExtrapolationTimeSimTicks.
        /// </summary>
        InterpolateAndExtrapolate = 3
    }

    /// <summary>
    /// Disabled enableable components still replicate their fields. Use GhostEnabledBit to replicate the enabled flag itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class GhostFieldAttribute : Attribute
    {
        /// <summary>
        /// Floats are multiplied by this value and rounded to integers. Zero sends full precision; integer fields do not support quantization.
        /// </summary>
        public int Quantization { get; set; } = -1;

        /// <summary>
        /// For nested structs, true uses one change bit for the whole struct; false uses one per field.
        /// </summary>
        public bool Composite { get; set; } = false;

        public SmoothingAction Smoothing { get; set; } = SmoothingAction.Clamp;

        public int SubType { get; set; } = 0;
        public bool SendData { get; set; } = true;

        /// <summary>
        /// Smoothing stops above this snapshot delta. For quaternions, specify sin(theta / 2) for the maximum smoothing angle.
        /// </summary>
        public float MaxSmoothingDistance { get; set; } = 0;
    }

    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, Inherited = true)]
    public class DontSerializeForCommandAttribute : Attribute
    {
    }
}
#endif
