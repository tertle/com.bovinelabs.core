// <copyright file="SaveAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Savanna
{
    using System;

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
}
