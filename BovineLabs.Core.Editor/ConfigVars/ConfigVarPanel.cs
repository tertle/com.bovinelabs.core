// <copyright file="ConfigVarPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> A panel that draws a collection of config vars. </summary>
    public sealed class ConfigVarPanel : ISettingsPanel
    {
        private const string FieldClassName = "config-var__field";
        private const string HighlightClassName = "search";
        private const string ReadOnlyClassName = "config-var__readonly";

        /// <summary> Initializes a new instance of the <see cref="ConfigVarPanel" /> class. </summary>
        /// <param name="displayName"> The display name of the panel. </param>
        public ConfigVarPanel(string displayName)
        {
            this.DisplayName = displayName;
        }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc/>
        public string GroupName => this.DisplayName;

        /// <inheritdoc/>
        public bool IsEmpty => false;

        /// <summary> Gets a list of all the config vars this panel draws. </summary>
        internal List<(ConfigVarAttribute ConfigVar, FieldInfo FieldInfo)> ConfigVars { get; } = new();

        private List<(ConfigVarAttribute ConfigVar, VisualElement Field)> Fields { get; } = new();

        /// <inheritdoc />
        public void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Matching the display name should show everything
            var allMatch = string.IsNullOrWhiteSpace(searchContext);

            foreach (var (attribute, fieldInfo) in this.ConfigVars)
            {
                var readOnly = attribute.IsReadOnly && EditorApplication.isPlaying;
                var field = CreateVisualElement(attribute, fieldInfo);

                field.AddToClassList(FieldClassName);
                var shouldHighlight = !allMatch && MatchesSearchContext(attribute.Name, searchContext);
                field.EnableInClassList(HighlightClassName, shouldHighlight);
                field.EnableInClassList(ReadOnlyClassName, readOnly);

                this.Fields.Add((attribute, field));
                rootElement.Add(field);
            }

            EditorApplication.playModeStateChanged += this.OnPlayModeStateChanged;
            this.OnPlayModeStateChanged(EditorApplication.isPlaying ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode);
        }

        /// <inheritdoc />
        public void OnDeactivate()
        {
            this.Fields.Clear();

            EditorApplication.playModeStateChanged -= this.OnPlayModeStateChanged;
        }

        /// <inheritdoc />
        bool ISettingsPanel.MatchesFilter(string searchContext, bool allowEmpty)
        {
            if (!allowEmpty && this.IsEmpty)
            {
                return false;
            }

            if (string.IsNullOrEmpty(searchContext))
            {
                return true;
            }

            return this.ConfigVars.Any(s => MatchesSearchContext(s.ConfigVar.Name, searchContext));
        }

        private static bool MatchesSearchContext(string s, string searchContext)
        {
            return s.IndexOf(searchContext, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private static VisualElement CreateVisualElement(ConfigVarAttribute configVar, FieldInfo field)
        {
            var fieldValue = field.GetValue(null);

            return fieldValue switch
            {
                SharedStatic<int> sharedStatic => SetupField(new IntegerField(), configVar, sharedStatic),
                SharedStatic<float> sharedStatic => SetupField(new FloatField(), configVar, sharedStatic),
                SharedStatic<bool> sharedStatic => SetupField(new Toggle(), configVar, sharedStatic),
                SharedStatic<Color> sharedStatic => SetupColorField(configVar, sharedStatic),
                SharedStatic<Vector4> sharedStatic => SetupVector4Field(configVar, sharedStatic),
                SharedStatic<Rect> sharedStatic => SetupRectField(configVar, sharedStatic),
                SharedStatic<FixedString32Bytes> sharedStatic => SetupTextField(configVar, sharedStatic),
                SharedStatic<FixedString64Bytes> sharedStatic => SetupTextField(configVar, sharedStatic),
                SharedStatic<FixedString128Bytes> sharedStatic => SetupTextField(configVar, sharedStatic),
                SharedStatic<FixedString512Bytes> sharedStatic => SetupTextField(configVar, sharedStatic),
                SharedStatic<FixedString4096Bytes> sharedStatic => SetupTextField(configVar, sharedStatic),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private static BaseField<T> SetupField<T>(BaseField<T> field, ConfigVarAttribute configVar, SharedStatic<T> sharedStatic)
            where T : unmanaged, IEquatable<T>
        {
            return SetupField(field, configVar, new ConfigVarBinding<T>(field, configVar, sharedStatic));
        }

        private static BaseField<string> SetupTextField<T>(ConfigVarAttribute configVar, SharedStatic<T> sharedStatic)
            where T : unmanaged, IEquatable<T>
        {
            var field = new TextField();
            return SetupField(field, configVar, new ConfigVarStringBinding<T>(field, configVar, sharedStatic));
        }

        private static BaseField<Color> SetupColorField(ConfigVarAttribute configVar, SharedStatic<Color> sharedStatic)
        {
            var field = new ColorField();
            return SetupField(field, configVar, new ConfigVarColorBinding(field, configVar, sharedStatic));
        }

        private static BaseField<Vector4> SetupVector4Field(ConfigVarAttribute configVar, SharedStatic<Vector4> sharedStatic)
        {
            var field = new Vector4Field();
            return SetupField(field, configVar, new ConfigVarVector4Binding(field, configVar, sharedStatic));
        }

        private static BaseField<Rect> SetupRectField(ConfigVarAttribute configVar, SharedStatic<Rect> sharedStatic)
        {
            var field = new RectField();
            return SetupField(field, configVar, new ConfigVarRectBinding(field, configVar, sharedStatic));
        }


        private static BaseField<T> SetupField<T>(BaseField<T> field, ConfigVarAttribute configVar, IConfigVarBinding<T> binding)
        {
            field.binding = binding;
            field.label = configVar.Name;
            field.tooltip = configVar.Description;
            field.value = binding.Value;
            return field;
        }

        private static void UpdateState(VisualElement field, ConfigVarAttribute configVar, bool isPlaying)
        {
            var isEnabled = !configVar.IsReadOnly || !isPlaying;
            field.SetEnabled(isEnabled);
            field.EnableInClassList(ReadOnlyClassName, !isEnabled);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode))
            {
                return;
            }

            var isPlaying = EditorApplication.isPlaying;
            foreach (var (configVar, field) in this.Fields)
            {
                UpdateState(field, configVar, isPlaying);
            }
        }
    }
}
