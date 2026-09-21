namespace BovineLabs.Core.UI
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Scripting.LifecycleManagement;
    using UnityEngine;
    using UnityEngine.UIElements;

    public static class BovineThemeUtility
    {
        public const string PreferenceKey = "BovineLabs.UI.Theme";
        public const string RootClass = "bl-theme";
        public const string WindowClass = "bl-theme-window";

        private const string WorksClass = "bl-theme--bovine-works";
        private const string CuratorClass = "bl-theme--curator";

        [NoAutoStaticsCleanup]
        private static readonly ConditionalWeakTable<VisualElement, ThemeBinding> Bindings = new();

        [NoAutoStaticsCleanup]
        private static BovineTheme theme = LoadInitialTheme();

        [NoAutoStaticsCleanup]
        private static StyleSheet styleSheet;

        /// <summary>
        /// Unsubscribe UI listeners when detached.
        /// </summary>
        [NoAutoStaticsCleanup]
        public static event Action<BovineTheme> ThemeChanged;

        public static BovineTheme Theme
        {
            get => theme;
            set
            {
                if (value != BovineTheme.BovineWorks && value != BovineTheme.Curator)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BovineLabs theme.");
                }

                if (theme == value)
                {
                    return;
                }

                theme = value;
#if !UNITY_EDITOR
                PlayerPrefs.SetInt(PreferenceKey, (int)value);
                PlayerPrefs.Save();
#endif
                ThemeChanged?.Invoke(value);
            }
        }

        public static void Apply(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Bindings.GetValue(root, element => new ThemeBinding(element)).Refresh();
        }

        private static BovineTheme LoadInitialTheme()
        {
#if UNITY_EDITOR
            // BovineThemePreferences restores the saved EditorPrefs choice during editor initialization.
            return BovineTheme.BovineWorks;
#else
            // Preferences are external, user-owned state and can outlive the themes shipped by an application.
            var saved = PlayerPrefs.GetInt(PreferenceKey, (int)BovineTheme.BovineWorks);
            return saved == (int)BovineTheme.Curator ? BovineTheme.Curator : BovineTheme.BovineWorks;
#endif
        }

        private sealed class ThemeBinding
        {
            private readonly VisualElement _root;

            public ThemeBinding(VisualElement root)
            {
                _root = root;
                root.RegisterCallback<AttachToPanelEvent>(OnAttach);
                root.RegisterCallback<DetachFromPanelEvent>(OnDetach);
                if (root.panel != null)
                {
                    ThemeChanged += OnThemeChanged;
                }
            }

            public void Refresh()
            {
                if (styleSheet == null)
                {
                    styleSheet = CoreSettings.I.ThemeStyleSheet;
                    if (styleSheet == null)
                    {
                        throw new InvalidOperationException("The required theme stylesheet is missing from CoreSettings.");
                    }
                }

                if (!_root.styleSheets.Contains(styleSheet))
                {
                    _root.styleSheets.Add(styleSheet);
                }

                _root.AddToClassList(RootClass);
                OnThemeChanged(Theme);
            }

            private void OnAttach(AttachToPanelEvent evt)
            {
                ThemeChanged -= OnThemeChanged;
                ThemeChanged += OnThemeChanged;
                Refresh();
            }

            private void OnDetach(DetachFromPanelEvent evt)
            {
                ThemeChanged -= OnThemeChanged;
            }

            private void OnThemeChanged(BovineTheme value)
            {
                _root.EnableInClassList(WorksClass, value == BovineTheme.BovineWorks);
                _root.EnableInClassList(CuratorClass, value == BovineTheme.Curator);
            }
        }
    }
}
