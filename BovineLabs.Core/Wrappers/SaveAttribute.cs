// <copyright file="SaveAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_SAVANNA
namespace BovineLabs.Savanna
{
    using System;

    /// <summary>Features for save components.</summary>
    [Flags]
    public enum SaveFeature : byte
    {
        /// <summary>No extra save behavior.</summary>
        None = 0,

        /// <summary>Allows the component to be added during load.</summary>
        AddComponent = 1,
    }

    /// <summary>Marks a component or buffer element as eligible for Savanna saving.</summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class SaveAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="SaveAttribute"/> class.</summary>
        /// <param name="feature">The save feature flags.</param>
        public SaveAttribute(SaveFeature feature = SaveFeature.None)
        {
            this.Feature = feature;
        }

        /// <summary>Gets the save feature flags.</summary>
        public SaveFeature Feature { get; }
    }

    /// <summary>Marks a field to be skipped when loading a saved component value.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveIgnoreAttribute : Attribute
    {
    }
}
#endif
