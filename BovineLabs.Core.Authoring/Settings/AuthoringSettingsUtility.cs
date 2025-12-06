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

            var result = TryGetSettings<T>(type, out var settings);
            if (!result)
            {
                throw new Exception($"Settings not found for {typeof(T)}, ensure they've been created by opening the settings window");
            }

            CachedSettings.Add(type, settings!);
            return settings!;
        }

        /// <summary> Gets a settings file. Create if it doesn't exist and ensures it is setup properly. </summary>
        /// <param name="settings"> The settings if found. </param>
        /// <typeparam name="T"> The settings type. </typeparam>
        /// <returns> True if settings is created. </returns>
        /// <exception cref="Exception"> Thrown if more than 1 instance found in project. </exception>
        public static bool TryGetSettings<T>(out T? settings)
            where T : ScriptableObject, ISettings
        {
            var type = typeof(T);
            return TryGetSettings(type, out settings);
        }

        private static bool TryGetSettings<T>(Type type, out T? settings)
            where T : ScriptableObject, ISettings
        {
            var filter = type.Namespace == null ? type.Name : $"{type.Namespace}.{type.Name}";
            var assets = AssetDatabase.FindAssets($"t:{filter}");

            if (assets.Length == 0)
            {
                settings = null;
                return false;
            }

            settings = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(assets.First()));
            return true;
        }
    }
}
