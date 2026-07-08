// <copyright file="ObjectManagementImportExtensionAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Asset
{
    using System;

    /// <summary> Registers a non-.asset extension that should be processed by object management after import. </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class ObjectManagementImportExtensionAttribute : Attribute
    {
        public ObjectManagementImportExtensionAttribute(string extension)
        {
            this.Extension = extension;
        }

        public string Extension { get; }
    }
}
