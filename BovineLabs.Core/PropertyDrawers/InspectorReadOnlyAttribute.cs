// <copyright file="InspectorReadOnlyAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
    }
}
