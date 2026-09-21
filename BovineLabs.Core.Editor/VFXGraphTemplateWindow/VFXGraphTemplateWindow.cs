#if UNITY_VFX_GRAPH
namespace BovineLabs.Core.Editor.VFXGraphTemplateWindow
{
    using BovineLabs.Core.Editor.UI;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.VFX;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.VFX;

    public class VFXGraphTemplateWindow : EditorWindow
    {
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/VFXGraphTemplateWindow/";
        [NoAutoStaticsCleanup]
        private static readonly UITemplate Window = new(RootUIPath + "VFXGraphTemplateWindow");

        private TextField _nameField;
        private TextField _categoryField;
        private TextField _descriptionField;

        [MenuItem(EditorMenus.RootMenuTools + "Create VFX Template", priority = -15)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<VFXGraphTemplateWindow>();
            window.titleContent = new GUIContent("BovineLabs");
            window.Show();
        }

        private void OnEnable()
        {
            var root = rootVisualElement;

            Window.Clone(root);

            rootVisualElement.Q<Button>().clicked += CreateTemplate;
            _nameField = rootVisualElement.Q<TextField>("Name");
            _categoryField = rootVisualElement.Q<TextField>("Category");
            _descriptionField = rootVisualElement.Q<TextField>("Description");
        }

        private void CreateTemplate()
        {
            if (!TryGetPath(out var path))
            {
                BLGlobalLogger.LogErrorString("No VisualEffectAsset selected");
                return;
            }

            if (string.IsNullOrWhiteSpace(_nameField!.value))
            {
                BLGlobalLogger.LogErrorString("No name set");
                return;
            }

            VFXTemplateHelper.TrySetTemplate(path, new VFXTemplateDescriptor
            {
                name = _nameField.value,
                category = _categoryField!.value,
                description = _descriptionField!.value,
            });
        }

        private static bool TryGetPath(out string path)
        {
            var asset = Selection.activeObject as VisualEffectAsset;
            if (!asset)
            {
                path = null;
                return false;
            }

            path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path);
        }
    }
}
#endif
