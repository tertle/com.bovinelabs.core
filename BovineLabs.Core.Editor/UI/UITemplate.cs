namespace BovineLabs.Core.Editor.UI
{
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>
    /// Adapted from Unity.Entities.Editor.UITemplate.
    /// </summary>
    public readonly struct UITemplate
    {
        private readonly string _uxmlPath;
        private readonly string _ussPath;

        private const string k_ProSuffix = "_dark";
        private const string k_PersonalSuffix = "_light";

        public static string SkinSuffix => EditorGUIUtility.isProSkin ? k_ProSuffix : k_PersonalSuffix;

        public UITemplate(string path)
        {
            _uxmlPath = $"{path}.uxml";
            _ussPath = $"{path}.uss";
        }

        private VisualTreeAsset Template => (VisualTreeAsset)EditorGUIUtility.Load(_uxmlPath);

        private StyleSheet StyleSheet => AssetDatabase.LoadAssetAtPath<StyleSheet>(_ussPath);

        public VisualElement Clone(VisualElement root = null)
        {
            root = CloneTemplate(root);
            AddStyleSheetSkinVariant(root);
            return root;
        }

        private VisualElement CloneTemplate(VisualElement element = null)
        {
            if (element == null)
            {
                return Template.CloneTree();
            }

            Template.CloneTree(element);
            return element;
        }

        private void AddStyleSheetSkinVariant(VisualElement element)
        {
            if (StyleSheet == null)
            {
                return;
            }

            if (element == null)
            {
                return;
            }

            element.styleSheets.Add(StyleSheet);
            var assetPath = AssetDatabase.GetAssetPath(StyleSheet);
            assetPath = assetPath.Insert(assetPath.LastIndexOf('.'), SkinSuffix);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath) is var skin && skin != null)
            {
                element.styleSheets.Add(skin);
            }
        }
    }
}
