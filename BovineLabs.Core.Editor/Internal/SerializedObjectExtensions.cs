// <copyright file="SerializedObjectExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using System.Reflection;
    using UnityEditor;

    public static class SerializedObjectExtensions
    {
        public static void InspectorMode(this SerializedObject serialized, InspectorMode inspectorMode)
        {
            var property = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
            property.SetValue(serialized, inspectorMode);
        }
    }
}
