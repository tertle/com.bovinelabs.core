// <copyright file="PrefabElementAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    /// <summary> Attribute to draw the prefab instead of the instance. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PrefabElementAttribute : PropertyAttribute
    {
    }
}
