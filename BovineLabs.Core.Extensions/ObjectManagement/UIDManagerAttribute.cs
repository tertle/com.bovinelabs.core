// <copyright file="UIDManagerAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIDManagerAttribute : Attribute
    {
        public UIDManagerAttribute(string manager, string property)
        {
            this.Manager = manager;
            this.Property = property;
        }

        public string Manager { get; }

        public string Property { get; }
    }
}
#endif
