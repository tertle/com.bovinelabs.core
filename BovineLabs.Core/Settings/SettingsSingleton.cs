// <copyright file="SettingsSingleton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    public abstract class SettingsSingleton<T> : SettingsSingleton
        where T : SettingsSingleton
    {
        private static T settings;

        public static T I
        {
            get => GetSingleton(ref settings)!;
            private set => settings = value;
        }

        protected sealed override void Initialize()
        {
            Assert.AreEqual(this.GetType(), typeof(T));
            I = this as T;

            this.OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
    }

    [Serializable]
    public abstract class SettingsSingleton : ScriptableObject, ISettings
    {
        public virtual bool IncludeInBuild => true;

        // Simple helper for setting up singletons
        protected static T GetSingleton<T>(ref T field)
            where T : SettingsSingleton
        {
            if (!field)
            {
                field = CreateInstance<T>();
            }

            return field;
        }

        protected abstract void Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void LoadAll()
        {
#if UNITY_EDITOR
            var settingsGuids = AssetDatabase.FindAssets($"t:{nameof(SettingsSingleton)} a:all");

            foreach (var guid in settingsGuids)
            {
                var settingsPath = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<SettingsSingleton>(settingsPath);
#else
            var kvSettings = Resources.FindObjectsOfTypeAll<SettingsSingleton>();
            foreach (var setting in kvSettings)
            {
#endif
                setting.Initialize();
            }
        }

#if UNITY_EDITOR
        internal static void InitializeInEditor()
        {
            LoadAll();
        }
#endif
    }
}
