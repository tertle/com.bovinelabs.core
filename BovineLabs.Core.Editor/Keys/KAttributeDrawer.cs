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
        private static Dictionary<string, Type>? kTypes;

        protected override IEnumerable<(string Name, int Value)>? GetKeyValues(KAttribute attr)
        {
            var type = TryGetType(attr.Settings);
            if (type == null)
            {
                BLGlobalLogger.LogWarningString($"KAttribute could not find settings {attr.Settings}. Please check spelling and capitalization.");
                return null;
            }

            var settings = EditorSettingsUtility.GetSettings(type) as KSettingsBase;
            if (!settings)
            {
                return null;
            }

            if (settings is KSettingsBase<int> i)
            {
                return i.Keys.Select(k => (k.Name, k.Value));
            }

            if (settings is KSettingsBase<uint> ui)
            {
                return ui.Keys.Select(k => (k.Name, (int)k.Value));
            }

            if (settings is KSettingsBase<byte> b)
            {
                return b.Keys.Select(k => (k.Name, (int)k.Value));
            }

            if (settings is KSettingsBase<sbyte> sb)
            {
                return sb.Keys.Select(k => (k.Name, (int)k.Value));
            }

            if (settings is KSettingsBase<short> s)
            {
                return s.Keys.Select(k => (k.Name, (int)k.Value));
            }

            if (settings is KSettingsBase<ushort> us)
            {
                return us.Keys.Select(k => (k.Name, (int)k.Value));
            }

            BLGlobalLogger.LogWarningString("KAttribute is currently only supported on int, uint, byte, sbyte, short and ushort.");

            return null;
        }

        private static Type? TryGetType(string type)
        {
            if (kTypes == null)
            {
                kTypes = new Dictionary<string, Type>();

                foreach (var c in ReflectionUtility.GetAllImplementations<KSettingsBase>())
                {
                    kTypes[c.Name] = c;
                }
            }

            kTypes.TryGetValue(type, out var result);

            return result;
        }
    }
}
