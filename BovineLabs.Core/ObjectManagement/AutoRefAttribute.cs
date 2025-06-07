// <copyright file="AutoRefAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
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
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoRefAttribute : Attribute
    {
        public AutoRefAttribute(string managerType, string fieldName)
            : this(managerType, fieldName, null, null, null, false)
        {
            this.ManagerType = managerType;
            this.FieldName = fieldName;
        }

        public AutoRefAttribute(string managerType, string fieldName, string name, string subDirectory, bool createNull = true)
            : this(managerType, fieldName, $"bl.ar.{name.ToLowerNoSpaces()}", Path.Combine("Assets/Settings/",subDirectory),
                $"{name.FirstCharToUpper()}.asset", createNull)
        {
        }

        public AutoRefAttribute(
            string managerType, string fieldName, string directoryKey, string defaultDirectory, string defaultFileName, bool createNull = true)
        {
            this.ManagerType = managerType;
            this.FieldName = fieldName;

            this.DirectoryKey = directoryKey;
            this.DefaultDirectory = defaultDirectory;
            this.DefaultFileName = defaultFileName;
            this.CreateNull = createNull;
        }


        public string ManagerType { get; }

        public string FieldName { get; }

        public string DirectoryKey { get; }

        public string DefaultDirectory { get; }

        public string DefaultFileName { get; }

        public bool CreateNull { get; }
    }
}
