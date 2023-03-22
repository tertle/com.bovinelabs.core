// <copyright file="FeatureWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.FeatureWindow
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.UI;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class FeatureWindow : EditorWindow
    {
        private const string ExtensionsEnableKey = "BL_CORE_EXTENSIONS";
        private const string ExtensionsDisabledStyle = "enable-extensions-disabled";
        private const string ExtensionsEnabledStyle = "enable-extensions-enabled";

        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/FeatureWindow/";
        private static readonly UITemplate Window = new(RootUIPath + "FeatureWindow");
        private static readonly UITemplate FeatureTemplate = new(RootUIPath + "FeatureTemplate");

        [MenuItem("BovineLabs/Extensions", priority = -5)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<FeatureWindow>();
            window.titleContent = new GUIContent("BovineLab");
            window.Show();
        }

        private void OnEnable()
        {
            var root = this.rootVisualElement;
            Window.Clone(root);

            this.SetupEnableExtensionsButton();

            var view = this.rootVisualElement.Q<ScrollView>();




            // this.rootVisualElement.Query<Toggle>(className: "assembly").ForEach(this.BindAssemblyToggle);
            // this.rootVisualElement.Q<Button>("create").clickable.clicked += this.Create;
            // this.rootVisualElement.Q<TextField>("name").value = $"{PlayerSettings.companyName}.";
            // this.rootVisualElement.Q<TextField>("directory").SetEnabled(false);
        }

        private void SetupEnableExtensionsButton()
        {
            var enableExtensions = this.rootVisualElement.Q<Button>("EnableExtensions");

            SetEnabledExtensionState(enableExtensions);

            enableExtensions.clicked += () =>
            {
                var enable = !HasDefines(ExtensionsEnableKey);
                if (enable)
                {
                    AddDefine(ExtensionsEnableKey);
                }
                else
                {
                    RemoveDefine(ExtensionsEnableKey);
                }

                SetEnabledExtensionState(enableExtensions);
            };
        }

        private static void SetEnabledExtensionState(Button button)
        {
            var enabled = HasDefines(ExtensionsEnableKey);
            SetState(button, enabled, ExtensionsEnabledStyle, ExtensionsDisabledStyle);
            button.text = enabled ? "Extensions Enabled" : "Extensions Disabled";
        }

        private static void SetState(VisualElement button, bool isEnabled, string enabledStyle, string disabledStyle)
        {
            button.RemoveFromClassList(isEnabled ? disabledStyle : enabledStyle);
            button.AddToClassList(isEnabled ? enabledStyle : disabledStyle);
        }

        private static NamedBuildTarget GetTarget()
        {
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            return UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        private static string[] GetDefines()
        {
            PlayerSettings.GetScriptingDefineSymbols(GetTarget(), out var defines);
            return defines;
        }

        private static bool HasDefines(string define)
        {
            return GetDefines().Contains(define);
        }

        private static void AddDefine(string define)
        {
            var target = GetTarget();
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
            var list = new List<string>();
            list.AddRange(defines);
            list.Add(define);
            PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
        }

        private static void RemoveDefine(string define)
        {
            var target = GetTarget();
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
            var list = new List<string>();
            list.AddRange(defines);
            list.Remove(define);
            PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
        }
    }
}
