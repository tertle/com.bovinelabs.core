// <copyright file="SettingsBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Base class for simple settings. In general use one of the generic implementations for ease of use. </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class SettingsBase : ScriptableObject, ISettings
    {
        public abstract void Bake(IBaker baker);
    }
}
