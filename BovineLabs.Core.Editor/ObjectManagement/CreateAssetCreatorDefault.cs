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

    [InitializeOnLoad]
    public static class CreateAssetCreatorDefault
    {
        static CreateAssetCreatorDefault()
        {
            EditorApplication.delayCall += DelayCall;
        }

        private static void DelayCall()
        {
            // Stop auto creating settings
            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out _))
            {
                return;
            }

            // TODO this actually means it won't create null assets until after a domain reload and there is a chance a user could create an asset before then
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
