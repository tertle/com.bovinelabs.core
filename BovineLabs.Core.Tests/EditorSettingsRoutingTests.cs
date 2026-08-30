// <copyright file="EditorSettingsRoutingTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Settings;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using CoreEditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    public class EditorSettingsRoutingTests
    {
        private CoreEditorSettings editorSettings;
        private ClientMenuSettings settings;
        private GameObject defaultSettingsObject;
        private GameObject netCodeSettingsObject;
        private GameObject menuSettingsObject;
        private SettingsAuthoring defaultSettings;
        private SettingsAuthoring netCodeSettings;
        private SettingsAuthoring menuSettings;

        [SetUp]
        public void SetUp()
        {
            this.editorSettings = ScriptableObject.CreateInstance<CoreEditorSettings>();
            this.settings = ScriptableObject.CreateInstance<ClientMenuSettings>();
            this.defaultSettings = CreateAuthoring("Default Settings", out this.defaultSettingsObject);
            this.netCodeSettings = CreateAuthoring("NetCode Settings", out this.netCodeSettingsObject);
            this.menuSettings = CreateAuthoring("Menu Settings", out this.menuSettingsObject);

            ConfigureEditorSettings(this.editorSettings, this.defaultSettings, this.netCodeSettings, this.menuSettings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(this.menuSettingsObject);
            Object.DestroyImmediate(this.netCodeSettingsObject);
            Object.DestroyImmediate(this.defaultSettingsObject);
            Object.DestroyImmediate(this.settings);
            Object.DestroyImmediate(this.editorSettings);
        }

        [TestCase("Client")]
        [TestCase("SERVER")]
        public void TryGetAuthoring_WithConfiguredRoute_ReturnsConfiguredAuthoring(string world)
        {
            Assert.That(this.editorSettings.TryGetAuthoring(world, out var authoring), Is.True);
            Assert.That(authoring, Is.SameAs(this.netCodeSettings));
        }

#if !UNITY_NETCODE
        [TestCase("Client")]
        [TestCase("SERVER")]
        public void TryGetAuthoring_WithoutNetCodeAndConfiguredRouteMissing_ReturnsDefault(string world)
        {
            ConfigureEditorSettings(this.editorSettings, this.defaultSettings, null, this.menuSettings);

            Assert.That(this.editorSettings.TryGetAuthoring(world, out var authoring), Is.True);
            Assert.That(authoring, Is.SameAs(this.defaultSettings));
        }
#endif

        [TestCase(false)]
        [TestCase(true)]
        public void AddSettingsToAuthoring_WithPartialMultiWorldAssignment_FillsMissingAuthoring(bool netCodeRouteContainsSetting)
        {
            if (netCodeRouteContainsSetting)
            {
                SetSettings(this.defaultSettings, this.settings);
                SetSettings(this.netCodeSettings, this.settings);
            }
            else
            {
                SetSettings(this.menuSettings, this.settings);
            }

            EditorSettingsUtility.AddSettingsToAuthoring(this.editorSettings, this.settings);

            Assert.That(CountSettings(this.netCodeSettings, this.settings), Is.EqualTo(1));
            Assert.That(CountSettings(this.menuSettings, this.settings), Is.EqualTo(1));
        }

#if !UNITY_NETCODE
        [TestCase(false)]
        [TestCase(true)]
        public void AddSettingsToAuthoring_WithoutNetCodeAndClientRouteMissing_FillsDefaultAndMenu(bool defaultRouteContainsSetting)
        {
            ConfigureEditorSettings(this.editorSettings, this.defaultSettings, null, this.menuSettings);

            if (defaultRouteContainsSetting)
            {
                SetSettings(this.defaultSettings, this.settings);
            }
            else
            {
                SetSettings(this.menuSettings, this.settings);
            }

            EditorSettingsUtility.AddSettingsToAuthoring(this.editorSettings, this.settings);

            Assert.That(CountSettings(this.defaultSettings, this.settings), Is.EqualTo(1));
            Assert.That(CountSettings(this.menuSettings, this.settings), Is.EqualTo(1));
        }
#endif

        private static SettingsAuthoring CreateAuthoring(string name, out GameObject gameObject)
        {
            gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            return gameObject.AddComponent<SettingsAuthoring>();
        }

        private static void ConfigureEditorSettings(
            CoreEditorSettings settings,
            SettingsAuthoring defaultAuthoring,
            SettingsAuthoring netCodeAuthoring,
            SettingsAuthoring menuAuthoring)
        {
            var serializedObject = new SerializedObject(settings);
            serializedObject.FindProperty("defaultSettingsAuthoring").objectReferenceValue = defaultAuthoring;

            var authorings = serializedObject.FindProperty("settingAuthoring");
            authorings.arraySize = netCodeAuthoring ? 3 : 1;

            if (netCodeAuthoring)
            {
                SetAuthoring(authorings.GetArrayElementAtIndex(0), "client", netCodeAuthoring);
                SetAuthoring(authorings.GetArrayElementAtIndex(1), "server", netCodeAuthoring);
                SetAuthoring(authorings.GetArrayElementAtIndex(2), "menu", menuAuthoring);
            }
            else
            {
                SetAuthoring(authorings.GetArrayElementAtIndex(0), "menu", menuAuthoring);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAuthoring(SerializedProperty property, string world, SettingsAuthoring authoring)
        {
            property.FindPropertyRelative("World").stringValue = world;
            property.FindPropertyRelative("Authoring").objectReferenceValue = authoring;
        }

        private static void SetSettings(SettingsAuthoring authoring, params SettingsBase[] settings)
        {
            var serializedObject = new SerializedObject(authoring);
            var settingsProperty = serializedObject.FindProperty("settings");
            settingsProperty.arraySize = settings.Length;

            for (var i = 0; i < settings.Length; i++)
            {
                settingsProperty.GetArrayElementAtIndex(i).objectReferenceValue = settings[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int CountSettings(SettingsAuthoring authoring, SettingsBase setting)
        {
            var serializedObject = new SerializedObject(authoring);
            var settingsProperty = serializedObject.FindProperty("settings");
            var count = 0;

            for (var i = 0; i < settingsProperty.arraySize; i++)
            {
                if (settingsProperty.GetArrayElementAtIndex(i).objectReferenceValue == setting)
                {
                    count++;
                }
            }

            return count;
        }

        [SettingsWorld("Client", "Menu")]
        private sealed class ClientMenuSettings : SettingsBase
        {
            public override void Bake(Baker<SettingsAuthoring> baker)
            {
            }
        }
    }
}
