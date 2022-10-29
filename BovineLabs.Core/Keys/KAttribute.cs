// <copyright file="KAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using UnityEngine;

    /// <summary> Apply to a byte/integer field to display the name defined in the <see cref="Settings"/> file. </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class KAttribute : PropertyAttribute
    {
        /// <summary> Initializes a new instance of the <see cref="KAttribute"/> class. </summary>
        /// <param name="settings"> The name of the settings file. </param>
        public KAttribute(string settings)
        {
            this.Settings = settings;
        }

        /// <summary> Gets or sets the name of the settings file. </summary>
        public string Settings { get; set; }
    }
}
