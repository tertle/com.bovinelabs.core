// <copyright file="UIDCreateAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class UIDCreateAttribute : PropertyAttribute
    {
    }
}
#endif
