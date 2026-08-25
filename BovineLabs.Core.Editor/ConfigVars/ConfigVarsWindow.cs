// <copyright file="ConfigVarsWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System.Linq;
    using BovineLabs.Core.ConfigVars;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Window for config vars. </summary>
    public class ConfigVarsWindow : EditorWindow
    {
        private const string StyleSheetPath = "Packages/com.bovinelabs.core/Editor Default Resources/ConfigVarsWindow/ConfigVarsWindow.uss";

        private readonly ConfigVarPanel panel = new();

        private ToolbarSearchField searchField;
        private VisualElement contentRoot;

        [MenuItem(EditorMenus.RootMenu + "ConfigVars", priority = -31)]
        internal static void OpenSettings()
        {
            Open();
        }

        internal static ConfigVarsWindow Open()
        {
            var window = Resources.FindObjectsOfTypeAll<ConfigVarsWindow>().FirstOrDefault() ?? CreateInstance<ConfigVarsWindow>();
            window.Show();
            window.Focus();
            window.minSize = new Vector2(450, 240);
            return window;
        }

        private void OnEnable()
        {
            this.titleContent = new GUIContent("ConfigVars", EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image);
            this.minSize = new Vector2(450, 240);

            this.SetupUI();
            this.RefreshConfigVars();

            EditorApplication.playModeStateChanged += this.OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            if (this.searchField != null)
            {
                this.searchField.UnregisterValueChangedCallback(this.OnSearchChanged);
            }

            this.panel.OnDeactivate();
            EditorApplication.playModeStateChanged -= this.OnPlayModeStateChanged;
        }

        private void SetupUI()
        {
            var root = this.rootVisualElement;
            root.Clear();
            root.AddToClassList("config-vars-window");

            if (AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath) is { } styleSheet)
            {
                root.styleSheets.Add(styleSheet);
            }

            var toolbar = new Toolbar();
            toolbar.AddToClassList("config-vars-window__toolbar");

            var resetButton = new ToolbarButton(this.ResetToDefault) { text = "Reset To Default" };
            toolbar.Add(resetButton);

            var spacer = new ToolbarSpacer();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            this.searchField = new ToolbarSearchField();
            this.searchField.AddToClassList("config-vars-window__search");
            if (this.searchField.Q<TextField>() is { } textField)
            {
                textField.isDelayed = true;
            }

            this.searchField.RegisterValueChangedCallback(this.OnSearchChanged);
            toolbar.Add(this.searchField);
            root.Add(toolbar);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("config-vars-window__scroll");
            root.Add(scrollView);

            this.contentRoot = new VisualElement();
            this.contentRoot.AddToClassList("config-vars-window__content");
            scrollView.Add(this.contentRoot);
        }

        private void RefreshConfigVars()
        {
            this.panel.SetConfigVars(ConfigVarManager.FindAllConfigVars());
            this.RefreshContent();
        }

        private void RefreshContent()
        {
            this.panel.Render(this.searchField?.value, this.contentRoot);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            this.RefreshContent();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode))
            {
                return;
            }

            this.panel.UpdatePlayModeState();
        }

        private void ResetToDefault()
        {
            if (!EditorUtility.DisplayDialog("Confirm Reset To Default", "Reset all config vars to default values?", "Reset", "Cancel"))
            {
                return;
            }

            foreach (var c in ConfigVarManager.All)
            {
                EditorPrefs.DeleteKey(ConfigVarManager.GetEditorPrefsKey(c.Key.Name));
                c.Value.StringValue = c.Key.DefaultValue;
            }

            this.RefreshContent();
        }
    }
}
