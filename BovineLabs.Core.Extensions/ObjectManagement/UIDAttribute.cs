// <copyright file="UIDAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using UnityEngine;

    public class UIDAttribute : PropertyAttribute
    {
        public UIDAttribute(string type)
        {
            this.Type = type;
        }

        public UIDAttribute(Type type)
        {
            this.Type = type.Name;
        }

        public string Type { get; }
    }
}
#endif
