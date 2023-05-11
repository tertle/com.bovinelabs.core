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
    }
}
