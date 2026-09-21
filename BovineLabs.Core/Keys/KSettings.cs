namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public abstract class KSettings<T, TV> : KSettingsBase<T, TV>
        where T : KSettings<T, TV>
        where TV : unmanaged, IEquatable<TV>
    {
        [SerializeField]
        private NameValue<TV>[] _keys = Array.Empty<NameValue<TV>>();

        public override IEnumerable<NameValue<TV>> Keys => _keys;

        public TV this[string key] => _keys.First(k => k.Name == key).Value;

        protected virtual IEnumerable<NameValue<TV>> SetReset()
        {
            return Enumerable.Empty<NameValue<TV>>();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _keys = SetReset().ToArray();
        }
#endif
    }
}
