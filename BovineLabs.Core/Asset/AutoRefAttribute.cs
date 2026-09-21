namespace BovineLabs.Core.Asset
{
    using System;
    using System.IO;
    using BovineLabs.Core.Extensions;

    /// <summary>
    /// On a ScriptableObject, assigns its instances to the named array field of the manager asset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class AutoRefAttribute : Attribute
    {
        public AutoRefAttribute(string managerType, string fieldName)
            : this(managerType, fieldName, null, null, null)
        {
            ManagerType = managerType;
            FieldName = fieldName;
        }

        public AutoRefAttribute(string managerType, string fieldName, string key, string subDirectory)
            : this(managerType, fieldName, NameToDirectory(key), Path.Combine("Assets/Settings/", subDirectory), $"{key.FirstCharToUpper()}.asset")
        {
        }

        public AutoRefAttribute(string managerType, string fieldName, string directoryKey, string defaultDirectory, string defaultFileName)
        {
            ManagerType = managerType;
            FieldName = fieldName;

            DirectoryKey = directoryKey;
            DefaultDirectory = defaultDirectory;
            DefaultFileName = defaultFileName;
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
