// <copyright file="UIDMetaAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class AssetCreatorAttribute : Attribute
    {
        public string DirectoryKey { get; }
        public string DefaultDirectory { get; }
        public string DefaultFileName { get; }

        public AssetCreatorAttribute(string directoryKey, string defaultDirectory, string defaultFileName)
        {
            this.DirectoryKey = directoryKey;
            this.DefaultDirectory = defaultDirectory;
            this.DefaultFileName = defaultFileName;
        }

    }
}
#endif
