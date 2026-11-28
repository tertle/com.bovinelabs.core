namespace BovineLabs.Core.Settings
{
    using System;
    using System.Linq;
    using Unity.Entities;
    using Unity.Profiling;
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

        [NoAutoStaticsCleanup]
        private static readonly ProfilerMarker LoadAllMarker = new("BovineLabs.Core.SettingsSingleton.LoadAll");

#if UNITY_EDITOR
        // Edit-mode systems and bakers need the Asset Database settings before Play Mode.
        [OnCodeInitializing]
#else
        // CoreBuildSetup's preloaded assets are available at this point in Player startup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        private static void LoadAll()
        {
            using var marker = LoadAllMarker.Auto();

            foreach (var setting in FindAll(typeof(SettingsSingleton)))
            {
                setting.Initialize();
            }
        }

        private static SettingsSingleton[] FindAll(Type type)
        {
#if UNITY_EDITOR
            var guids = type == typeof(SettingsSingleton)
                ? SettingsSingletonAssetCache.FindGuids()
                : AssetDatabase.FindAssets($"t:{type.Name} a:all");
            return guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<SettingsSingleton>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(type.IsInstanceOfType)
                .ToArray();
#else
            return Resources.FindObjectsOfTypeAll(type).Cast<SettingsSingleton>().ToArray();
#endif
        }
    }
}
