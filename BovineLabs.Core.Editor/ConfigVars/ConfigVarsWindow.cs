namespace BovineLabs.Core.Editor.ConfigVars
{
    using System.Linq;
    using BovineLabs.Core.ConfigVars;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ConfigVarsWindow : EditorWindow
    {
        private const string StyleSheetPath = "Packages/com.bovinelabs.core/Editor Default Resources/ConfigVarsWindow/ConfigVarsWindow.uss";

        private readonly ConfigVarPanel _panel = new();

        private ToolbarSearchField _searchField;
        private VisualElement _contentRoot;

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
            titleContent = new GUIContent("ConfigVars", EditorGUIUtility.IconContent("VerticalLayoutGroup Icon").image);
            minSize = new Vector2(450, 240);

            SetupUI();
            RefreshConfigVars();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            if (_searchField != null)
            {
                _searchField.UnregisterValueChangedCallback(OnSearchChanged);
            }

            _panel.OnDeactivate();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void SetupUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList("config-vars-window");

            if (AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath) is { } styleSheet)
            {
                root.styleSheets.Add(styleSheet);
            }

            var toolbar = new Toolbar();
            toolbar.AddToClassList("config-vars-window__toolbar");

            var resetButton = new ToolbarButton(ResetToDefault) { text = "Reset To Default" };
            toolbar.Add(resetButton);

            var spacer = new ToolbarSpacer();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            _searchField = new ToolbarSearchField();
            _searchField.AddToClassList("config-vars-window__search");
            if (_searchField.Q<TextField>() is { } textField)
            {
                textField.isDelayed = true;
            }

            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(_searchField);
            root.Add(toolbar);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("config-vars-window__scroll");
            root.Add(scrollView);

            _contentRoot = new VisualElement();
            _contentRoot.AddToClassList("config-vars-window__content");
            scrollView.Add(_contentRoot);
        }

        private void RefreshConfigVars()
        {
            _panel.SetConfigVars(ConfigVarManager.FindAllConfigVars());
            RefreshContent();
        }

        private void RefreshContent()
        {
            _panel.Render(_searchField?.value, _contentRoot);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RefreshContent();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode))
            {
                return;
            }

            _panel.UpdatePlayModeState();
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

            RefreshContent();
        }
    }
}
