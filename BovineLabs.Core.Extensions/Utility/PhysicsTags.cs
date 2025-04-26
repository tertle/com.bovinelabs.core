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
    public class PhysicsTags : KSettingsBase<PhysicsTags, int>
    {
        private readonly List<NameValue<int>> keys = new();

        [SerializeField]
        private CustomPhysicsMaterialTagNames tags;

        public override IEnumerable<NameValue<int>> Keys
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

                    this.keys.Add(new NameValue<int>(tag, i));
                }

                return this.keys;
            }
        }
    }
}
#endif
