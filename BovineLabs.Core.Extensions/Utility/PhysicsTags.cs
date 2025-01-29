// <copyright file="PhysicsTags.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_PHYSICS_CUSTOM
namespace BovineLabs.Core
{
    using System.Collections.Generic;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;
    using Unity.Physics.Authoring;
    using UnityEngine;

    [SettingsGroup("Core")]
    public class PhysicsTags : KSettings
    {
        private readonly List<NameValue> keys = new();

        [SerializeField]
        private CustomPhysicsMaterialTagNames tags;

        public override IReadOnlyList<NameValue> Keys
        {
            get
            {
                this.keys.Clear();

                if (this.tags == null)
                {
                    return this.keys;
                }

                for (var i = 0; i < this.tags.TagNames.Count; i++)
                {
                    var tag = this.tags.TagNames[i];
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        continue;
                    }

                    this.keys.Add(new NameValue
                    {
                        Name = tag,
                        Value = i,
                    });
                }

                return this.keys;
            }
        }

        protected override void Initialize()
        {
            K<PhysicsTags>.Initialize(this.Keys);
        }
    }
}
#endif
