// <copyright file="ObjectCategories.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    /// <summary> Object categories dynamically implement using <see cref="K{T}" />. </summary>
    [SettingsGroup("Core")]
    public class ObjectCategories : KSettings
    {
        [SerializeField]
        private ComponentMap[] keys = Array.Empty<ComponentMap>();

        public IReadOnlyCollection<ComponentMap> Components => this.keys;

        public override IReadOnlyList<NameValue> Keys => this
            .keys
            .Select(k => new NameValue
            {
                Name = k.Name,
                Value = k.Value,
            })
            .ToArray();

        /// <inheritdoc />
        protected override void Initialize()
        {
            K<ObjectCategories>.Initialize(this.Keys);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Validate(ref this.keys);
        }
#endif

        [Serializable]
        public struct ComponentMap : IKKeyValue
        {
            [SerializeField]
            private string name;

            [SerializeField]
            [Range(0, 31)]
            private int value;

            [SerializeField]
            [StableTypeHash(StableTypeHashAttribute.TypeCategory.ComponentData, OnlyZeroSize = true, AllowUnityNamespace = false)]
            private ulong component;

            public string Name
            {
                get => this.name;
                set => this.name = value;
            }

            public int Value => this.value;

            /// <inheritdoc />
            int IKKeyValue.Value
            {
                get => this.value;
                set => this.value = (byte)value;
            }

            public ulong ComponentType => this.component;
        }
    }
}
#endif
