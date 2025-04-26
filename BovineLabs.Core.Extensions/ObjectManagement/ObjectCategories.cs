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

    /// <summary> Object categories dynamically implemented. </summary>
    [SettingsGroup("Core")]
    public class ObjectCategories : KSettingsBase<ObjectCategories, byte>
    {
        [SerializeField]
        private ComponentMap[] keys = Array.Empty<ComponentMap>();

        public IReadOnlyCollection<ComponentMap> Components => this.keys;

        public override IEnumerable<NameValue<byte>> Keys => this.keys.Select(k => new NameValue<byte>(k.Name, k.Value));

        [Serializable]
        public struct ComponentMap
        {
            [SerializeField]
            private string name;

            [SerializeField]
            [Range(0, 31)]
            private byte value;

            [SerializeField]
            [StableTypeHash(StableTypeHashAttribute.TypeCategory.ComponentData, OnlyZeroSize = true, AllowUnityNamespace = false)]
            private ulong component;

            public string Name => this.name;

            public byte Value => this.value;

            public ulong ComponentType => this.component;
        }
    }
}
#endif
