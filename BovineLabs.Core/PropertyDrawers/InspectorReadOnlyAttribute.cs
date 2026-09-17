namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorReadOnlyAttribute : PropertyAttribute
    {
    }
}
