// <copyright file="KSettingsT.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary> Generic implementation of <see cref="KSettings" /> to allow calling the generic <see cref="K{T}" />. </summary>
    /// <typeparam name="T"> Itself. </typeparam>
    public abstract class KSettings<T> : KSettings
        where T : KSettings<T>
    {
        [SerializeField]
        private NameValue[] keys = Array.Empty<NameValue>();

        public override IReadOnlyList<NameValue> Keys => this.keys;

        /// <inheritdoc />
        protected sealed override void Initialize()
        {
            K<T>.Initialize(this.keys);
        }

        protected virtual IEnumerable<NameValue> SetReset()
        {
            return Enumerable.Empty<NameValue>();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Validate(ref this.keys);
        }

        private void Reset()
        {
            this.keys = this.SetReset().ToArray();
        }
#endif
    }
}
