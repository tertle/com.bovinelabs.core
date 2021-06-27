// <copyright file="Settings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Base class for simple settings. In general use one of the generic implementations for ease of use. </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class Settings : ScriptableObject, ISettings
    {
        /// <inheritdoc />
        public abstract void Convert(EntityManager dstManager, Entity entity);
    }
}