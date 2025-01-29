// <copyright file="ResourceSettingsAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;

    /// <summary> Attribute that will put the settings in the Resource folder. </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ResourceSettingsAttribute : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="ResourceSettingsAttribute" /> class. </summary>
        /// <param name="directory"> An optional subdirectory to use in resources. </param>
        public ResourceSettingsAttribute(string directory = "")
        {
            this.Directory = directory;
        }

        public string Directory { get; }
    }
}
