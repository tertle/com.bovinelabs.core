// <copyright file="AuthoringSettingsUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEngine;

    /// <summary> Utility for setting up and getting settings. </summary>
    public static class AuthoringSettingsUtility
    {
        private static readonly Dictionary<Type, ISettings> CachedSettings = new();

        /// <summary> Gets a settings file. </summary>
        /// <typeparam name="T"> The type. </typeparam>
        /// <returns> The settings instance. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static T GetSettings<T>()
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);

            if (CachedSettings.TryGetValue(type, out var cached))
            {
                return (T)cached;
            }

            var settings = GetSettings<T>(type);
            CachedSettings.Add(type, settings);
            return settings;
        }

        private static T GetSettings<T>(Type type)
            where T : ScriptableObject, ISettings
        {
            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assets.First()));
        }
    }
}
