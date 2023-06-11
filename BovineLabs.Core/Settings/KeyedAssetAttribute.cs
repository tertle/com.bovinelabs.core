// <copyright file="KeyedAssetAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using UnityEngine;

    public class KeyedAssetAttribute : PropertyAttribute
    {
        public KeyedAssetAttribute(string type)
        {
            this.Type = type;
        }

        public KeyedAssetAttribute(Type type)
        {
            this.Type = type.Name;
        }

        public string Type { get; }
    }
}
