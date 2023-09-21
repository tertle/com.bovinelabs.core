// <copyright file="SettingsGroupAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsGroupAttribute : Attribute
    {
        public SettingsGroupAttribute(string group)
        {
            this.Group = group;
        }

        public string Group { get; }
    }
}
