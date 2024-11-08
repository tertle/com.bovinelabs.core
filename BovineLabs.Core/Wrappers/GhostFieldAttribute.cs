// <copyright file="GhostFieldAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    /// <summary>Denotes how <see cref="GhostFieldAttribute"/> values are deserialized when received from snapshots.</summary>
    public enum SmoothingAction
    {
        /// <summary>The GhostField value will clamp to the latest snapshot value as it's available.</summary>
        Clamp = 0,

        /// <summary>Interpolate the GhostField value between the latest two processed snapshot values, and if no data is available for the next tick, clamp at the latest snapshot value.
        /// Tweak the <see cref="ClientTickRate"/> interpolation values if too jittery, or too delayed.</summary>
        Interpolate = 1 << 0,

        /// <summary>
        /// Interpolate the GhostField value between snapshot values, and if no data is available for the next tick, the next value is linearly extrapolated using the previous two snapshot values.
        /// Extrapolation is limited (i.e. clamped) via <see cref="ClientTickRate.MaxExtrapolationTimeSimTicks"/>.
        /// </summary>
        InterpolateAndExtrapolate = 3
    }

    /// <summary>
    /// Attribute used to specify how and which fields and properties of <see cref="Unity.Entities.IComponentData"/> or
    /// <see cref="Unity.Entities.IBufferElementData"/> should be replicated.
    /// When a component or buffer contains at least one field that is annotated with a <see cref="GhostFieldAttribute"/>,
    /// a struct implementing the component serialization is automatically code-generated.
    /// </summary>
    /// <remarks>Note that "enableable components" (<see cref="Unity.Entities.IEnableableComponent"/>) will still have their fields replicated, even when disabled.
    /// See <see cref="GhostEnabledBitAttribute"/> to replicate the enabled flag itself.</remarks>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
    public class GhostFieldAttribute : Attribute
    {
        /// <summary>
        /// Floating point numbers will be multiplied by this number and rounded to an integer, enabling better delta-compression via huffman encoding.
        /// Quantization is not supported for integer numbers and is disabled by default for floats.
        /// To send a floating point number unquantized, use 0.
        /// Examples:
        /// Quantization=0 implies full precision.
        /// Quantization=1 implies precision of 1f (i.e. round float values to integers).
        /// Quantization=2 implies precision of 0.5f.
        /// Quantization=10 implies precision of 0.1f.
        /// Quantization=20 implies precision of 0.05f.
        /// Quantization=1000 implies precision of 0.001f.
        /// </summary>
        public int Quantization { get; set; } = -1;

        /// <summary>
        /// Only applicable on GhostFieldAttributes applied to a non primitive struct containing multiple fields.
        /// If this value is not set (a.k.a. false, the default), a 'change bit' will be included 'per field, for every field inside the nested struct'.
        /// There will be no 'change bit' for the struct itself.
        /// I.e. If a single field inside the sub-struct changes, only that fields 'change bit' will be set.
        /// Otherwise (if this Composite bool is set, a.k.a. true), we instead use a single 'change bit' for 'the entire nested struct'.
        /// I.e. If any fields inside the sub-struct change, the single 'change bit' for the entire struct will be set.
        /// Check the Serialize/Deserialize code-generated methods in Library\NetCodeGenerated_Backup for examples.
        /// </summary>
        public bool Composite { get; set; } = false;

        /// <summary>
        /// Default is <see cref="SmoothingAction.Clamp"/>.
        /// </summary>
        /// <inheritdoc cref="SmoothingAction"/>
        public SmoothingAction Smoothing { get; set; } = SmoothingAction.Clamp;

        /// <summary>Allows you to specify a custom serializer for this GhostField using the <see cref="GhostFieldSubType"/> API.</summary>
        /// <inheritdoc cref="GhostFieldSubType"/>
        public int SubType { get; set; } = 0;
        /// <summary>
        /// Default true. If unset (false), instructs code-generation to not include this field in the serialization data.
        /// I.e. Do not replicate this field.
        /// This is particularly useful for non primitive members (like structs), which will have all fields serialized by default.
        /// </summary>
        public bool SendData { get; set; } = true;

        /// <summary>
        /// The maximum distance between two snapshots for which smoothing will be applied.
        /// If the value changes more than this between two received snapshots the smoothing
        /// action will not be performed.
        /// </summary>
        /// <remarks>
        /// For quaternions the value specified should be sin(theta / 2) - where theta is the maximum angle
        /// you want to apply smoothing for.
        /// </remarks>
        public float MaxSmoothingDistance { get; set; } = 0;
    }

    /// <summary>
    /// Attribute denoting that an <see cref="Unity.Entities.IEnableableComponent"/> should have its enabled flag replicated.
    /// And thus, this is only valid on enableable component types. You'll get compiler errors if it's not.
    /// </summary>
    /// <remarks>A type will not replicate its enableable flag unless it has this attribute attached to the class.
    /// This can (and should) also be added to variants that serialize enable bits.</remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GhostEnabledBitAttribute : Attribute
    {
    }

    /// <summary>
    /// Add the attribute to prevent a field ICommandData struct to be serialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, Inherited = true)]
    public class DontSerializeForCommandAttribute : Attribute
    {
    }
}
#endif
