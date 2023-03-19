// <copyright file="KSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary> Generic implementation of <see cref="KSettings" /> to allow calling the generic <see cref="K{T}" />. </summary>
    /// <typeparam name="T"> Itself. </typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Makes sense")]
    public abstract class KSettings<T> : KSettings
        where T : KSettings<T>
    {
        internal sealed override void Init()
        {
            K<T>.Initialize(this.Keys);
        }
    }

    /// <summary>
    /// The base KSettings file for defining custom enums, layers, keys. Do not implement this directly, implement <see cref="KSettings{T}" />.
    /// </summary>
    [Serializable]
    public abstract class KSettings : ScriptableObject, ISettings
    {
        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;

        [SerializeField]
        private NameValue[] keys = Array.Empty<NameValue>();

        public NameValue[] Keys => this.keys;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (this.keys.Length > KMap.MaxCapacity)
            {
                var keysOld = this.keys;
                this.keys = new NameValue[KMap.MaxCapacity];
                Array.Copy(keysOld, this.keys, KMap.MaxCapacity);
            }

            for (var i = 0; i < this.keys.Length; i++)
            {
                var k = this.keys[i];
                k.Name = k.Name.ToLower();
                this.keys[i] = k;
            }
        }
#endif

        internal abstract void Init();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadAll()
        {
            var kvSettings = Resources.LoadAll<KSettings>(string.Empty);

            foreach (var setting in kvSettings)
            {
                setting.Init();
            }
        }
    }
}
