namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class SettingsBase : ScriptableObject, ISettings
    {
        public abstract void Bake(Baker<SettingsAuthoring> baker);
    }
}
