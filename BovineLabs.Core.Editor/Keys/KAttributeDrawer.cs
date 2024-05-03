// <copyright file="KAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(KAttribute), true)]
    public class KAttributeDrawer : BitFieldAttributeEditor<KAttribute>
    {
        private static Dictionary<string, Type>? KTypes;

        protected override IEnumerable<(string Name, int Value)>? GetKeyValues(KAttribute attr)
        {
            var type = TryGetType(attr.Settings);
            if (type == null)
            {
                Debug.LogWarning($"KAttribute could not find settings {attr.Settings}. Please check spelling and capitalization.");
                return null;
            }

            var k = EditorSettingsUtility.GetSettings(type) as KSettings;
            return k == null ? null : k.Keys.Select(s => (s.Name, s.Value));
        }

        private static Type? TryGetType(string type)
        {
            if (KTypes == null)
            {
                KTypes = new Dictionary<string, Type>();

                foreach (var c in ReflectionUtility.GetAllImplementations<KSettings>())
                {
                    KTypes[c.Name] = c;
                }
            }

            KTypes.TryGetValue(type, out var result);

            return result;
        }
    }
}
