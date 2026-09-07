// <copyright file="BovineThemeTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.UI
{
    using System.Collections;
    using System.Linq;
    using BovineLabs.Core.UI;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;

    public class BovineThemeTests
    {
        private EditorWindow window;
        private BovineTheme originalTheme;
        private bool hadPreference;
        private int originalPreference;

        [SetUp]
        public void SetUp()
        {
            this.originalTheme = BovineThemeUtility.Theme;
            this.hadPreference = EditorPrefs.HasKey(BovineThemeUtility.PreferenceKey);
            this.originalPreference = EditorPrefs.GetInt(BovineThemeUtility.PreferenceKey);
            BovineThemeUtility.Theme = BovineTheme.BovineWorks;
            this.window = ScriptableObject.CreateInstance<EditorWindow>();
            this.window.titleContent = new GUIContent("Theme test");
            this.window.ShowUtility();
        }

        [TearDown]
        public void TearDown()
        {
            this.window.Close();
            BovineThemeUtility.Theme = this.originalTheme;
            if (this.hadPreference)
            {
                EditorPrefs.SetInt(BovineThemeUtility.PreferenceKey, this.originalPreference);
            }
            else
            {
                EditorPrefs.DeleteKey(BovineThemeUtility.PreferenceKey);
            }
        }

        [UnityTest]
        public IEnumerator ChangingThemeUpdatesOptedInScopeAndSelectorWithoutRestylingSiblings()
        {
            var scope = new BovineThemeRoot();
            scope.AddToClassList(BovineThemeUtility.WindowClass);
            var selector = new BovineThemeSelector();
            scope.Add(selector);
            var ordinaryButton = new Button { text = "Unrelated UI" };
            this.window.rootVisualElement.Add(ordinaryButton);
            this.window.rootVisualElement.Add(scope);
            yield return null;

            var ordinaryBackground = ordinaryButton.resolvedStyle.backgroundColor;
            Assert.That(scope.resolvedStyle.backgroundColor, Is.EqualTo((Color)new Color32(27, 25, 24, 255)));

            selector.Q<DropdownField>().value = "The Curator";
            yield return null;

            Assert.That(BovineThemeUtility.Theme, Is.EqualTo(BovineTheme.Curator));
            Assert.That(scope.resolvedStyle.backgroundColor, Is.EqualTo((Color)new Color32(32, 43, 53, 255)));
            Assert.That(ordinaryButton.resolvedStyle.backgroundColor, Is.EqualTo(ordinaryBackground));
            Assert.That(this.window.rootVisualElement.ClassListContains(BovineThemeUtility.RootClass), Is.False);

            BovineThemeUtility.Theme = BovineTheme.BovineWorks;
            yield return null;
            Assert.That(selector.Q<DropdownField>().value, Is.EqualTo("Bovine Works"));
        }

        [UnityTest]
        public IEnumerator SwitchingPreferencePagesLeavesSharedHostAndNextPageUnthemed()
        {
            var factory = TypeCache.GetMethodsWithAttribute<SettingsProviderAttribute>()
                .Single(method => method.DeclaringType?.FullName == "BovineLabs.Core.Editor.UI.BovineThemePreferences");
            var provider = (SettingsProvider)factory.Invoke(null, null);
            var host = new VisualElement();
            host.AddToClassList("settings-panel");
            this.window.rootVisualElement.Add(host);
            var ordinaryButton = new Button { text = "Another preference page" };
            host.Add(ordinaryButton);
            yield return null;

            var hostBackground = host.resolvedStyle.backgroundColor;
            var buttonBackground = ordinaryButton.resolvedStyle.backgroundColor;
            var hostStyleCount = host.styleSheets.count;
            host.Clear();
            provider.OnActivate(string.Empty, host);
            yield return null;

            var selector = host.Q<BovineThemeSelector>();
            selector.Q<DropdownField>().value = "The Curator";
            yield return null;

            Assert.That(host.Q<BovineThemeRoot>().resolvedStyle.backgroundColor, Is.EqualTo((Color)new Color32(32, 43, 53, 255)));
            Assert.That(host.resolvedStyle.backgroundColor, Is.EqualTo(hostBackground));
            Assert.That(host.styleSheets.count, Is.EqualTo(hostStyleCount));
            Assert.That(host.GetClasses(), Is.EquivalentTo(new[] { "settings-panel" }));

            provider.OnDeactivate();
            host.Clear();
            host.Add(ordinaryButton);
            BovineThemeUtility.Theme = BovineTheme.BovineWorks;
            yield return null;

            Assert.That(host.resolvedStyle.backgroundColor, Is.EqualTo(hostBackground));
            Assert.That(ordinaryButton.resolvedStyle.backgroundColor, Is.EqualTo(buttonBackground));
            Assert.That(selector.Q<DropdownField>().value, Is.EqualTo("The Curator"));
        }

        [UnityTest]
        public IEnumerator ReattachingScopeUsesLatestThemeAndPreservesExistingStyleSheets()
        {
            var scope = new VisualElement();
            var ownStyle = ScriptableObject.CreateInstance<StyleSheet>();
            try
            {
                scope.styleSheets.Add(ownStyle);
                BovineThemeUtility.Apply(scope);
                BovineThemeUtility.Apply(scope);
                scope.AddToClassList(BovineThemeUtility.WindowClass);
                var selector = new BovineThemeSelector();
                scope.Add(selector);
                this.window.rootVisualElement.Add(scope);
                yield return null;

                scope.RemoveFromHierarchy();
                BovineThemeUtility.Theme = BovineTheme.Curator;
                Assert.That(selector.Q<DropdownField>().value, Is.EqualTo("Bovine Works"));

                this.window.rootVisualElement.Add(scope);
                yield return null;

                Assert.That(scope.resolvedStyle.backgroundColor, Is.EqualTo((Color)new Color32(32, 43, 53, 255)));
                Assert.That(selector.Q<DropdownField>().value, Is.EqualTo("The Curator"));
                Assert.That(scope.styleSheets.Contains(ownStyle), Is.True);
                Assert.That(scope.styleSheets.count, Is.EqualTo(2));
            }
            finally
            {
                scope.RemoveFromHierarchy();
                Object.DestroyImmediate(ownStyle);
            }
        }
    }
}
