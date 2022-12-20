// <copyright file="BufferCapacitySettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    public class BufferCapacitySettings : ScriptableObject, ISettings
    {
        [SerializeField]
        private NewBufferCapacity[] bufferCapacities;

        public IReadOnlyList<NewBufferCapacity> BufferCapacities => this.bufferCapacities;

        [Serializable]
        public struct NewBufferCapacity
        {
            [StableTypeHash(StableTypeHashAttribute.TypeCategory.BufferData)]
            public ulong StableHash;

            public int Capacity;
        }
    }
}
