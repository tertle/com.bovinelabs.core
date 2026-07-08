// <copyright file="AutoRefAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Asset
{
    using System;
    using System.IO;
    using BovineLabs.Core.Extensions;

    /// <summary>
    /// When applied to a ScriptableObject, this attribute ensures that any instance of the object is
    /// automatically assigned to the specified field (which is expected to be an array) of the given manager asset.
    /// This is often used in combination with AssetCreator.
    /// </summary>
    /// <example>
    ///     <code>
    /// public class Manager : ScriptableObject
    /// {
    ///     [SerializeField]
    ///     public DataSchema[] Data;
    /// }
    ///
    /// [AutoRef(nameof(Manager), nameof(Manager.Data))]
    /// public class DataSchema : ScriptableObject
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoRefAttribute : Attribute
    {
        public AutoRefAttribute(string managerType, string fieldName)
            : this(managerType, fieldName, null, null, null)
        {
            this.ManagerType = managerType;
            this.FieldName = fieldName;
        }

        public AutoRefAttribute(string managerType, string fieldName, string key, string subDirectory)
            : this(managerType, fieldName, NameToDirectory(key), Path.Combine("Assets/Settings/", subDirectory), $"{key.FirstCharToUpper()}.asset")
        {
        }

        public AutoRefAttribute(
            string managerType, string fieldName, string directoryKey, string defaultDirectory, string defaultFileName)
        {
            this.ManagerType = managerType;
            this.FieldName = fieldName;

            this.DirectoryKey = directoryKey;
            this.DefaultDirectory = defaultDirectory;
            this.DefaultFileName = defaultFileName;
        }

        public string ManagerType { get; }

        public string FieldName { get; }

        public string ReferenceFieldName { get; set; }

        public string DirectoryKey { get; }

        public string DefaultDirectory { get; }

        public string DefaultFileName { get; }

        public static string NameToDirectory(string name) => $"bl.ar.{name.ToLowerNoSpaces()}";
    }
}
