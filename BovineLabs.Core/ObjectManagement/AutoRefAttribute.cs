// <copyright file="AutoRefAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoRefAttribute : Attribute
    {
        public AutoRefAttribute(string manager, string property)
        {
            this.Manager = manager;
            this.Property = property;
        }

        public string Manager { get; }

        public string Property { get; }
    }
}