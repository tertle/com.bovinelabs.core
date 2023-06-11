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
    using UnityEngine;

    /// <summary> Object categories dynamically implement using <see cref="K{T}" />. </summary>
    public class ObjectCategories : KSettings
    {
        [SerializeField]
        private ComponentMap[] keys = Array.Empty<ComponentMap>();

        public IReadOnlyCollection<ComponentMap> Components => this.keys;

        public override IReadOnlyList<NameValue> Keys => this.keys.Select(k => new NameValue() { Name = k.Name, Value = k.Value }).ToArray();

        protected internal override void Init()
        {
            K<ObjectCategories>.Initialize(this.Keys);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (this.keys.Length > KMap.MaxCapacity)
            {
                var keysOld = this.keys;
                this.keys = new ComponentMap[KMap.MaxCapacity];
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

        [Serializable]
        public struct ComponentMap
        {
            [SerializeField]
            private string name;

            [SerializeField]
            private byte value;

            [SerializeField]
            private ObjectGroup? objectGroup;

            [SerializeField]
            [StableTypeHash(StableTypeHashAttribute.TypeCategory.ComponentData, OnlyZeroSize = true, AllowUnityNamespace = false)]
            private ulong component;

            public string Name
            {
                get => this.name;
                internal set => this.name = value;
            }

            public byte Value => this.value;

            public ulong ComponentType => this.component;

            public ObjectGroup? ObjectGroup => this.objectGroup;
        }
    }
}
#endif
