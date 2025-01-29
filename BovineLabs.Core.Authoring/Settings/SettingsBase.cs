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
        /// <summary> Called in the baking process to bake the authoring component. </summary>
        /// <remarks> This method will be called by the <see cref="Baker{T}" /> for <see cref="SettingsAuthoring" />. </remarks>
        /// <param name="baker"> The <see cref="SettingsAuthoring" /> baker that is invoking this. </param>
        public abstract void Bake(Baker<SettingsAuthoring> baker);
    }
}
