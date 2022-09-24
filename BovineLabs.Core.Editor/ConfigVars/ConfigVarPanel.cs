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
    using Unity.Collections;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> A panel that draws a collection of config vars. </summary>
    public sealed class ConfigVarPanel : ISettingsPanel
    {
        /// <summary> Initializes a new instance of the <see cref="ConfigVarPanel"/> class. </summary>
        /// <param name="displayName"> The display name of the panel. </param>
        public ConfigVarPanel(string displayName)
        {
            this.DisplayName = displayName;
        }

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <summary> Gets a list of all the config vars this panel draws. </summary>
        internal List<(ConfigVarAttribute ConfigVar, IConfigVarContainer Container)> ConfigVars { get; }
            = new();

        /// <inheritdoc/>
        void ISettingsPanel.OnActivate(string searchContext, VisualElement rootElement)
        {
            // Matching the display name should show everything
            var allMatch = string.IsNullOrWhiteSpace(searchContext);

            foreach (var (attribute, container) in this.ConfigVars)
            {
                var readOnly = attribute.IsReadOnly && EditorApplication.isPlaying;
                var field = CreateVisualElement(attribute, container);

                // var.OnChanged += () => field.SetValueWithoutNotify(var.Value);

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

                rootElement.Add(field);
            }
        }

        /// <inheritdoc/>
        void ISettingsPanel.OnDeactivate()
        {
        }

        /// <inheritdoc/>
        bool ISettingsPanel.MatchesFilter(string searchContext)
        {
            return this.ConfigVars.Any(s => MatchesSearchContext(s.ConfigVar.Name, searchContext));
        }

        private static bool MatchesSearchContext(string s, string searchContext)
        {
            return s.IndexOf(searchContext, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private static VisualElement CreateVisualElement(ConfigVarAttribute configVar, IConfigVarContainer obj)
        {
            return obj switch
            {
                ConfigVarSharedStaticContainer<int> intField => SetupField(new IntegerField(), configVar, intField),
                ConfigVarSharedStaticContainer<float> floatField => SetupField(new FloatField(), configVar, floatField),
                ConfigVarSharedStaticContainer<bool> boolField => SetupField(new Toggle(), configVar, boolField),
                ConfigVarSharedStaticStringContainer<FixedString32Bytes> stringField32 => SetupTextField(new TextField(), configVar, stringField32),
                ConfigVarSharedStaticStringContainer<FixedString64Bytes> stringField64 => SetupTextField(new TextField(), configVar, stringField64),
                ConfigVarSharedStaticStringContainer<FixedString128Bytes> stringField128 => SetupTextField(new TextField(), configVar, stringField128),
                ConfigVarSharedStaticStringContainer<FixedString512Bytes> stringField512 => SetupTextField(new TextField(), configVar, stringField512),
                ConfigVarSharedStaticStringContainer<FixedString4096Bytes> stringField4096 => SetupTextField(new TextField(), configVar, stringField4096),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private static BaseField<T> SetupField<T>(BaseField<T> field, ConfigVarAttribute configVar, ConfigVarSharedStaticContainer<T> container)
            where T : struct, IEquatable<T>
        {
            field.binding = new ConfigVarBinding<T>(field, container);

            field.label = configVar.Name;
            field.tooltip = configVar.Description;
            field.value = container.DirectValue;

            if (field is TextInputBaseField<T> textInputBaseField)
            {
                textInputBaseField.isReadOnly = configVar.IsReadOnly;
                textInputBaseField.isDelayed = true;
            }

            field.RegisterValueChangedCallback(evt =>
            {
                container.DirectValue = evt.newValue;
                PlayerPrefs.SetString(configVar.Name, container.DirectValue.ToString());
            });
            return field;
        }

        private static BaseField<string> SetupTextField<T>(TextInputBaseField<string> field, ConfigVarAttribute configVar, ConfigVarSharedStaticStringContainer<T> container)
            where T : struct
        {
            field.binding = new SharedStaticTextFieldBind<T>(field, container);

            field.label = configVar.Name;
            field.tooltip = configVar.Description;
            field.value = container.Value;
            field.isReadOnly = configVar.IsReadOnly;
            field.isDelayed = true;

            field.RegisterValueChangedCallback(evt =>
            {
                container.Value = evt.newValue;
                PlayerPrefs.SetString(configVar.Name, container.Value);
            });

            return field;
        }
    }
}