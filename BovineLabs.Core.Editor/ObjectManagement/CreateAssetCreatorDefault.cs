// <copyright file="CreateAssetCreatorDefault.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System.Reflection;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEngine;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    internal static class CreateAssetCreatorDefault
    {
        internal static void Initialize()
        {
            EditorApplication.delayCall += DelayCall;
        }

        private static void DelayCall()
        {
            // Stop auto creating settings
            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out _))
            {
                // This should only happen on first installation of Core, once setup it should always find EditorSettings
                EditorApplication.delayCall += DelayCall;
                return;
            }

            CreateDefaults();
        }

        private static void CreateDefaults()
        {
            using (TimeProfiler.Start("CreateAssetCreatorDefault"))
            {
                foreach (var type in TypeCache.GetTypesWithAttribute<AutoRefAttribute>())
                {
                    if (!typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var attr = type.GetCustomAttribute<AutoRefAttribute>();
                    if (!attr.CreateNull)
                    {
                        continue;
                    }

                    if (AssetDatabase.FindAssets($"t:{type.Name}").Length > 0)
                    {
                        continue;
                    }

                    var path = OMUtility.GetNullDefaultPath(attr);
                    OMUtility.CreateInstance(type, path);
                }
            }
        }
    }
}
