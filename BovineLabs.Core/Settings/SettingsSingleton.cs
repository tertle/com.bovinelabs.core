namespace BovineLabs.Core.Settings
{
    using System;
    using System.Linq;
    using Unity.Entities;
    using Unity.Scripting.LifecycleManagement;
    using UnityEngine;
    using UnityEngine.Assertions;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public abstract class SettingsSingleton<T> : SettingsSingleton
        where T : SettingsSingleton
    {
        [NoAutoStaticsCleanup]
        private static T settings;

        [NoAutoStaticsCleanup]
        public static T I
        {
            get => GetSingleton(settings);
            private set => settings = value;
        }

        protected sealed override void Initialize()
        {
            Assert.AreEqual(GetType(), typeof(T));
            I = this as T;

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
    }

    [Serializable]
    public abstract partial class SettingsSingleton : ScriptableObject, ISettings
    {
        public virtual bool IncludeInBuild => true;

        // Managed readers can run before LoadAll, such as OnValidate during a code reload, so resolve the asset on first read.
        protected static T GetSingleton<T>(T field)
            where T : SettingsSingleton
        {
            if (field)
            {
                return field;
            }

            var settings = FindAll(typeof(T));
            if (settings.Length != 1)
            {
                throw new InvalidOperationException($"Expected one {typeof(T).Name} settings asset in the project or the build, found {settings.Length}.");
            }

            settings[0].Initialize();
            return (T)settings[0];
        }

        protected abstract void Initialize();

#if UNITY_EDITOR
        internal void InitializeCreatedAsset()
        {
            Initialize();
        }
#endif

        // Before Unity creates any system, so OnCreate and Burst readers of K settings see the real assets. Players find the
        // assets CoreBuildSetup adds to the preloaded assets.
        [OnCodeInitializing]
        private static void LoadAll()
        {
            foreach (var setting in FindAll(typeof(SettingsSingleton)))
            {
                setting.Initialize();
            }
        }

        private static SettingsSingleton[] FindAll(Type type)
        {
#if UNITY_EDITOR
            return AssetDatabase.FindAssets($"t:{type.Name} a:all")
                .Select(guid => AssetDatabase.LoadAssetAtPath<SettingsSingleton>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(type.IsInstanceOfType)
                .ToArray();
#else
            return Resources.FindObjectsOfTypeAll(type).Cast<SettingsSingleton>().ToArray();
#endif
        }
    }
}
