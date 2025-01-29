// <copyright file="KSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// The base KSettings file for defining custom enums, layers, keys. Do not implement this directly, implement <see cref="KSettings{T}" />.
    /// </summary>
    [Serializable]
    [ResourceSettings(KResourceDirectory)]
    public abstract class KSettings : ScriptableObject, ISettings
    {
        public const string KResourceDirectory = "K";

        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;

        public abstract IReadOnlyList<NameValue> Keys { get; }

        protected abstract void Initialize();

#if UNITY_EDITOR
        protected static void Validate<T>(ref T[] keys)
            where T : IKKeyValue
        {
            if (keys.Length > KMap.MaxCapacity)
            {
                var keysOld = keys;
                keys = new T[KMap.MaxCapacity];
                Array.Copy(keysOld, keys, KMap.MaxCapacity);
            }

            for (var i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                k.Name = k.Name.ToLower();
                k.Value = math.min(k.Value, KMap.MaxCapacity - 1);
                keys[i] = k;
            }
        }
#endif

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadAll()
        {
            var kvSettings = Resources.LoadAll<KSettings>(KResourceDirectory);

            foreach (var setting in kvSettings)
            {
                setting.Initialize();
            }
        }
    }
}
