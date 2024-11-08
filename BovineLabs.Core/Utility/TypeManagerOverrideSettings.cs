// <copyright file="TypeManagerOverrideSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_TYPEMANAGER_OVERRIDE
namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    [ResourceSettings]
    [SettingsGroup("Core")]
    public class TypeManagerOverrideSettings : ScriptableObject, ISettings
    {
        [SerializeField]
        private NewBufferCapacity[] bufferCapacities = Array.Empty<NewBufferCapacity>();

        [SerializeField]
        [StableTypeHash(StableTypeHashAttribute.TypeCategory.BufferData | StableTypeHashAttribute.TypeCategory.ComponentData)]
        private ulong[] enableables = Array.Empty<ulong>();

        public IReadOnlyList<NewBufferCapacity> BufferCapacities => this.bufferCapacities;

        public IReadOnlyList<ulong> Enableables => this.enableables;

        [Serializable]
        public struct NewBufferCapacity
        {
            [StableTypeHash(StableTypeHashAttribute.TypeCategory.BufferData)]
            public ulong StableHash;

            public int Capacity;
        }
    }
}
#endif
