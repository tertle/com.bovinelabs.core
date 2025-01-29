// <copyright file="FeatureWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.FeatureWindow
{
    using System.Collections.Generic;
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

        private const string FeatureDisabledStyle = "feature-disabled";
        private const string FeatureEnabledStyle = "feature-enabled";

        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/FeatureWindow/";
        private static readonly UITemplate Window = new(RootUIPath + "FeatureWindow");

        private readonly List<string> defines = new();
        private UQueryBuilder<VisualElement> features;

        [MenuItem("BovineLabs/Features", priority = -500)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<FeatureWindow>();
            window.titleContent = new GUIContent("BovineLabs");
            window.Show();
        }

        private void OnEnable()
        {
            this.defines.Clear();
            this.defines.AddRange(GetDefines());

            var root = this.rootVisualElement;
            Window.Clone(root);

            var view = this.rootVisualElement.Q<ScrollView>();
            this.features = view.Query<VisualElement>(className: "feature-group");

            this.SetupEnableExtensionsButton();
            this.SetupApplyButton();
            this.features.ForEach(this.SetupFeature);
        }

        private void SetupEnableExtensionsButton()
        {
            var enableExtensions = this.rootVisualElement.Q<Button>("EnableExtensions");

            this.SetEnabledExtensionState(enableExtensions, this.defines.Contains(ExtensionsEnableKey));

            enableExtensions.clicked += () =>
            {
                var enable = !this.defines.Contains(ExtensionsEnableKey);
                if (enable)
                {
                    this.defines.Add(ExtensionsEnableKey);
                }
                else
                {
                    this.defines.Remove(ExtensionsEnableKey);
                }

                this.SetEnabledExtensionState(enableExtensions, enable);
            };
        }

        private void SetupApplyButton()
        {
            this.rootVisualElement.Q<Button>("ApplyChanges").clicked += () => ApplyDefines(this.defines.ToArray());
        }

        private void SetupFeature(VisualElement e)
        {
            var extensionDisabledKey = e.Q<Label>().name;
            var button = e.Q<Button>();

            SetEnabledFeatureState(button, !this.defines.Contains(extensionDisabledKey));

            e.Q<Button>().clicked += () =>
            {
                var enable = this.defines.Contains(extensionDisabledKey);
                if (enable)
                {
                    this.defines.Remove(extensionDisabledKey);
                }
                else
                {
                    this.defines.Add(extensionDisabledKey);
                }

                SetEnabledFeatureState(button, enable);
            };
        }

        private void SetEnabledExtensionState(Button button, bool enabled)
        {
            SetState(button, enabled, ExtensionsEnabledStyle, ExtensionsDisabledStyle);
            button.text = enabled ? "Extensions Enabled" : "Extensions Disabled";

            this.features.ForEach(e => e.SetEnabled(enabled));
        }

        private static void SetEnabledFeatureState(Button button, bool enabled)
        {
            SetState(button, enabled, FeatureEnabledStyle, FeatureDisabledStyle);
            button.text = enabled ? "Enabled" : "Disabled";
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
            return NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        }

        private static string[] GetDefines()
        {
            PlayerSettings.GetScriptingDefineSymbols(GetTarget(), out var defines);
            return defines;
        }

        private static void ApplyDefines(string[] defines)
        {
            PlayerSettings.SetScriptingDefineSymbols(GetTarget(), defines);
        }
    }
}
