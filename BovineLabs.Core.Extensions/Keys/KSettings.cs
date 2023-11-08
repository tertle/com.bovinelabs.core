// <copyright file="KSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CONFIG
namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary> Generic implementation of <see cref="KSettings" /> to allow calling the generic <see cref="K{T}" />. </summary>
    /// <typeparam name="T"> Itself. </typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Makes sense")]
    public abstract class KSettings<T> : KSettings
        where T : KSettings<T>
    {
        [SerializeField]
        private NameValue[] keys = Array.Empty<NameValue>();

        public override IReadOnlyList<NameValue> Keys => this.keys;

        /// <inheritdoc />
        protected internal sealed override void Init()
        {
            K<T>.Initialize(this.keys);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Validate(ref this.keys);
        }
#endif
    }

    /// <summary>
    /// The base KSettings file for defining custom enums, layers, keys. Do not implement this directly, implement <see cref="KSettings{T}" />.
    /// </summary>
    [Serializable]
    [ResourceSettings("K")]
    public abstract class KSettings : ScriptableObject, ISettings
    {
        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;

        public abstract IReadOnlyList<NameValue> Keys { get; }

        protected internal abstract void Init();

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

            var all = new HashSet<int>();
            all.UnionWith(keys.Select(s => s.Value));

            var duplicate = new HashSet<int>();

            for (var i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                k.Name = k.Name.ToLower();

                if (!duplicate.Add(k.Value))
                {
                    var newKey = k.Value;
                    do
                    {
                        newKey++;
                    }
                    while (!all.Add(newKey));

                    k.Value = newKey;
                    duplicate.Add(k.Value);
                }

                keys[i] = k;
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadAll()
        {
            var kvSettings = Resources.LoadAll<KSettings>("K");

            foreach (var setting in kvSettings)
            {
                setting.Init();
            }
        }
    }
}
#endif
