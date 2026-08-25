// <copyright file="SettingsPrefabIdentity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using Unity.Entities;

    /// <summary> Identifies the source prefab of a settings root baked for the Editor. </summary>
    internal struct SettingsPrefabIdentity : IComponentData
    {
        /// <summary> The settings prefab asset GUID. </summary>
        public Hash128 PrefabGuid;
    }
}
