// <copyright file="BovineThemeUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Runtime.CompilerServices;
    using Unity.Scripting.LifecycleManagement;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>Applies the shared theme to an explicitly owned UI subtree.</summary>
    public static class BovineThemeUtility
    {
        public const string PreferenceKey = "BovineLabs.UI.Theme";
        public const string StyleSheetResource = "BovineLabs/Themes/BovineLabs";
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

        /// <summary>Raised after the shared theme changes. UI subscribers should unsubscribe when detached.</summary>
        [field: NoAutoStaticsCleanup]
        public static event Action<BovineTheme> ThemeChanged;

        /// <summary>Gets or sets the user's theme. Editor persistence is owned by the Core editor preference provider.</summary>
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

        /// <summary>Applies the selected theme now and keeps the subtree up to date while it belongs to a panel.</summary>
        /// <param name="root">The owned subtree. Its layout and background remain unchanged unless it opts into themed classes.</param>
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
            private readonly VisualElement root;

            public ThemeBinding(VisualElement root)
            {
                this.root = root;
                root.RegisterCallback<AttachToPanelEvent>(this.OnAttach);
                root.RegisterCallback<DetachFromPanelEvent>(this.OnDetach);
                if (root.panel != null)
                {
                    ThemeChanged += this.OnThemeChanged;
                }
            }

            public void Refresh()
            {
                if (styleSheet == null)
                {
                    styleSheet = Resources.Load<StyleSheet>(StyleSheetResource);
                    if (styleSheet == null)
                    {
                        throw new InvalidOperationException($"The required BovineLabs theme stylesheet '{StyleSheetResource}' is missing.");
                    }
                }

                if (!this.root.styleSheets.Contains(styleSheet))
                {
                    this.root.styleSheets.Add(styleSheet);
                }

                this.root.AddToClassList(RootClass);
                this.OnThemeChanged(Theme);
            }

            private void OnAttach(AttachToPanelEvent evt)
            {
                ThemeChanged -= this.OnThemeChanged;
                ThemeChanged += this.OnThemeChanged;
                this.Refresh();
            }

            private void OnDetach(DetachFromPanelEvent evt)
            {
                ThemeChanged -= this.OnThemeChanged;
            }

            private void OnThemeChanged(BovineTheme value)
            {
                this.root.EnableInClassList(WorksClass, value == BovineTheme.BovineWorks);
                this.root.EnableInClassList(CuratorClass, value == BovineTheme.Curator);
            }
        }
    }
}
