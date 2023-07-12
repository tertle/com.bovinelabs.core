// <copyright file="UIDManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using System;

    public class UIDManager : Attribute
    {
        public UIDManager(string manager, string property)
        {
            this.Manager = manager;
            this.Property = property;
        }

        public string Manager { get; }

        public string Property { get; }
    }
}
