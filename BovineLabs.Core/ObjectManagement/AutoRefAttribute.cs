// <copyright file="AutoRefAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using System;

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
        {
            this.ManagerType = managerType;
            this.FieldName = fieldName;
        }

        public string ManagerType { get; }

        public string FieldName { get; }
    }
}
