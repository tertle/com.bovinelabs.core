// <copyright file="IUIDCreateNull.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System.Reflection;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class CreateAssetCreatorDefault
    {
        static CreateAssetCreatorDefault()
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
