namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxAttribute : PropertyAttribute
    {
        public MinMaxAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }
}
