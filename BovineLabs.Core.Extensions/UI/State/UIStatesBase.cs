// <copyright file="UIStatesBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Keys;
    using UnityEngine;
    using UnityEngine.UIElements;

    public abstract class UIStatesBase : KSettings
    {
        [SerializeField]
        private NameUI[] keys = Array.Empty<NameUI>();

        public override IReadOnlyList<NameValue> Keys => this.keys.Select(k => new NameValue { Name = k.Name, Value = k.Value }).ToArray();

        public IReadOnlyList<NameUI> Data => this.keys;

        protected void SetKeys(NameUI[] newKeys)
        {
            this.keys = newKeys;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Validate(ref this.keys);
        }
#endif

        [Serializable]
        public class NameUI : IKKeyValue
        {
            [SerializeField]
            private string name;

            [SerializeField]
            private byte value;

            [SerializeField]
            private VisualTreeAsset? asset;

            public NameUI(string name, byte value, VisualTreeAsset asset)
            {
                this.name = name;
                this.Value = value;
                this.asset = asset;
            }

            public string Name
            {
                get => this.name;
                set => this.name = value;
            }

            public int Value
            {
                get => this.value;
                set => this.value = (byte)value;
            }

            public VisualTreeAsset? Asset => this.asset;
        }
    }
}
#endif
