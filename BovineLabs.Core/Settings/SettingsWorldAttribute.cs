// <copyright file="SettingsWorldAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsWorldAttribute : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="SettingsWorldAttribute"/> class. </summary>
        /// <param name="world"> The key matching EditorSettings. Not case-sensitive. </param>
        public SettingsWorldAttribute(string world)
        {
            this.World = world;
        }

        /// <summary> Gets the key matching EditorSettings. Not case-sensitive. </summary>
        public string World { get; }
    }
}
