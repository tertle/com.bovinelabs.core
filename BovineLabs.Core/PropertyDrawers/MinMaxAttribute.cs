// <copyright file="MinMaxAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class MinMaxAttribute : PropertyAttribute
    {
        public MinMaxAttribute(float min, float max)
        {
            this.Min = min;
            this.Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }
}
