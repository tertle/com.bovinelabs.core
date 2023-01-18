// <copyright file="ConfigVarPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary> A panel that draws a collection of config vars. </summary>
    public sealed class ConfigVarPanel : ISettingsPanel
    {
        /// <summary> Initializes a new instance of the <see cref="ConfigVarPanel" /> class. </summary>
        /// <param name="displayName"> The display name of the panel. </param>
        public ConfigVarPanel(string displayName)
        {
            this.DisplayName = displayName;
        }

        /// <inheritdoc />
        public string DisplayName { get; }

        /// <summary> Gets a list of all the config vars this panel draws. </summary>
        internal List<(ConfigVarAttribute ConfigVar, Type FieldType)> ConfigVars { get; } = new();

        private List<(ConfigVarAttribute ConfigVar, VisualElement Field)> Fields { get; } = new();

        /// <inheritdoc />
        public void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Matching the display name should show everything
            var allMatch = string.IsNullOrWhiteSpace(searchContext);

            foreach (var (attribute, fieldType) in this.ConfigVars)
            {
                var readOnly = attribute.IsReadOnly && EditorApplication.isPlaying;
                var field = CreateVisualElement(attribute, fieldType);

                // TODO move to uss
                if (!allMatch && MatchesSearchContext(attribute.Name, searchContext))
                {
                    field.style.backgroundColor = ConfigVarStyle.Style.HighlightColor;
                }

                if (readOnly)
                {
                    // TODO
                    // field.style.color = new StyleColor();
                }

                this.Fields.Add((attribute, field));
                rootElement.Add(field);
            }

            EditorApplication.playModeStateChanged += this.OnPlayModeStateChanged;
            this.OnPlayModeStateChanged(EditorApplication.isPlaying
                ? PlayModeStateChange.EnteredPlayMode
                : PlayModeStateChange.EnteredEditMode);
        }

        /// <inheritdoc />
        public void OnDeactivate()
        {
            this.Fields.Clear();

            EditorApplication.playModeStateChanged -= this.OnPlayModeStateChanged;
        }

        /// <inheritdoc />
        bool ISettingsPanel.MatchesFilter(string searchContext)
        {
            return this.ConfigVars.Any(s => MatchesSearchContext(s.ConfigVar.Name, searchContext));
        }

        private static bool MatchesSearchContext(string s, string searchContext)
        {
            return s.IndexOf(searchContext, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private static VisualElement CreateVisualElement(ConfigVarAttribute configVar, Type type)
        {
            if (type == typeof(SharedStatic<int>))
            {
                return SetupField(new IntegerField(), configVar);
            }

            if (type == typeof(SharedStatic<float>))
            {
                return SetupField(new FloatField(), configVar);
            }

            if (type == typeof(SharedStatic<bool>))
            {
                return SetupField(new Toggle(), configVar);
            }

            if ((type == typeof(SharedStatic<FixedString32Bytes>)) ||
                (type == typeof(SharedStatic<FixedString64Bytes>)) ||
                (type == typeof(SharedStatic<FixedString128Bytes>)) ||
                (type == typeof(SharedStatic<FixedString512Bytes>)) ||
                (type == typeof(SharedStatic<FixedString4096Bytes>)))
            {
                return SetupTextField(new TextField(), configVar);
            }

            throw new ArgumentOutOfRangeException();
        }

        private static BaseField<T> SetupField<T>(BaseField<T> field, ConfigVarAttribute configVar)
            where T : unmanaged, IEquatable<T>
        {
            return SetupField(field, configVar, new ConfigVarBinding<T>(field, configVar));
        }

        private static BaseField<string> SetupTextField(TextInputBaseField<string> field, ConfigVarAttribute configVar)
        {
            return SetupField(field, configVar, new ConfigVarStringBinding(field, configVar));
        }

        private static BaseField<T> SetupField<T>(BaseField<T> field, ConfigVarAttribute configVar, IConfigVarBinding<T> binding)
        {
            field.binding = binding;
            field.label = configVar.Name;
            field.tooltip = configVar.Description;
            field.value = binding.Value;
            field.RegisterValueChangedCallback(evt => EditorPrefs.SetString(configVar.Name, evt.newValue.ToString()));
            return field;
        }

        private static void UpdateState(VisualElement field, ConfigVarAttribute configVar, bool isPlaying)
        {
            var isEnabled = !configVar.IsReadOnly || !isPlaying;

            if (field is TextInputBaseField<string> textInputBaseField)
            {
                textInputBaseField.isReadOnly = !isEnabled;
                textInputBaseField.isDelayed = true;
            }
            else
            {
                field.SetEnabled(isEnabled);
            }
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
