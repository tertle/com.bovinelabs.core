// <copyright file="KSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <typeparam name="T"> Itself. </typeparam>
    /// <typeparam name="TV"> The value. </typeparam>
    public abstract class KSettings<T, TV> : KSettingsBase<T, TV>
        where T : KSettings<T, TV>
        where TV : unmanaged, IEquatable<TV>
    {
        [SerializeField]
        private NameValue<TV>[] keys = Array.Empty<NameValue<TV>>();

        public override IEnumerable<NameValue<TV>> Keys => this.keys;

        public TV this[string key] => this.keys.First(k => k.Name == key).Value;

        protected virtual IEnumerable<NameValue<TV>> SetReset()
        {
            return Enumerable.Empty<NameValue<TV>>();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            this.keys = this.SetReset().ToArray();
        }
#endif
    }
}
