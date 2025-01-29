// <copyright file="SettingsWorldAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsWorldAttribute : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="SettingsWorldAttribute" /> class. </summary>
        /// <param name="worlds"> The key matching EditorSettings. Not case-sensitive. </param>
        public SettingsWorldAttribute(params string[] worlds)
        {
            this.Worlds = worlds;
        }

        /// <summary> Gets the key matching EditorSettings. Not case-sensitive. </summary>
        public string[] Worlds { get; }
    }
}
