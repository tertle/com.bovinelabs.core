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
    using Unity.Burst;
    using Unity.Collections;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Draws config vars in a grouped single-column list. </summary>
    public sealed class ConfigVarPanel
    {
        private const string GroupClassName = "config-var-group";
        private const string GroupHeaderClassName = "config-var-group__header";
        private const string GroupNameClassName = "config-var-group__name";
        private const string RowClassName = "config-var__row";
        private const string FieldClassName = "config-var__field";
        private const string FieldLabelClassName = "config-var__field-label";
        private const string EmptyClassName = "config-var__empty";
        private const string HighlightClassName = "search";
        private const string ReadOnlyClassName = "config-var__readonly";

        private readonly List<ConfigVarEntry> configVars = new();
        private readonly List<FieldState> fields = new();

        /// <summary> Replaces the visible config vars rendered by this panel. </summary>
        /// <param name="configVars"> The config vars discovered by <see cref="ConfigVarManager" />. </param>
        internal void SetConfigVars(IEnumerable<(ConfigVarAttribute ConfigVar, FieldInfo Field)> configVars)
        {
            this.configVars.Clear();
            this.fields.Clear();

            foreach (var (configVar, fieldInfo) in configVars)
            {
                if (configVar.IsHidden)
                {
                    continue;
                }

                this.configVars.Add(new ConfigVarEntry(configVar, fieldInfo));
            }

            this.configVars.Sort(CompareEntries);
        }

        /// <summary> Renders matching config vars into the provided root. </summary>
        /// <param name="searchContext"> Search text used to filter names, groups, and descriptions. </param>
        /// <param name="rootElement"> The root element to fill. </param>
        internal void Render(string searchContext, VisualElement rootElement)
        {
            rootElement.Clear();
            this.fields.Clear();

            var filter = searchContext?.Trim() ?? string.Empty;
            var matching = this.configVars.Where(c => c.Matches(filter)).ToList();

            if (matching.Count == 0)
            {
                var empty = new Label("No config vars match the current search.");
                empty.AddToClassList(EmptyClassName);
                rootElement.Add(empty);
                return;
            }

            foreach (var group in matching.GroupBy(c => c.GroupName))
            {
                var groupRoot = new VisualElement();
                groupRoot.AddToClassList(GroupClassName);
                rootElement.Add(groupRoot);

                groupRoot.Add(CreateHeader(group.Key, GroupMatchesSearch(group.Key, filter)));

                foreach (var entry in group)
                {
                    groupRoot.Add(this.CreateRow(entry, filter));
                }
            }

            this.UpdatePlayModeState();
        }

        internal void OnDeactivate()
        {
            this.fields.Clear();
        }

        internal void UpdatePlayModeState()
        {
            var isPlaying = EditorApplication.isPlaying;
            foreach (var field in this.fields)
            {
                UpdateState(field.Row, field.Field, field.ConfigVar, isPlaying);
            }
        }

        private static int CompareEntries(ConfigVarEntry x, ConfigVarEntry y)
        {
            var group = string.Compare(x.GroupName, y.GroupName, StringComparison.Ordinal);
            return group != 0 ? group : string.Compare(x.ConfigVar.Name, y.ConfigVar.Name, StringComparison.Ordinal);
        }

        private static VisualElement CreateHeader(string groupName, bool highlight)
        {
            var header = new VisualElement();
            header.AddToClassList(GroupHeaderClassName);
            header.EnableInClassList(HighlightClassName, highlight);

            var name = new Label(groupName);
            name.AddToClassList(GroupNameClassName);
            header.Add(name);

            return header;
        }

        private VisualElement CreateRow(ConfigVarEntry entry, string filter)
        {
            var row = new VisualElement();
            row.AddToClassList(RowClassName);
            row.EnableInClassList(HighlightClassName, !string.IsNullOrWhiteSpace(filter));

            var field = CreateVisualElement(entry.ConfigVar, entry.FieldInfo);
            field.AddToClassList(FieldClassName);
            row.Add(field);

            this.fields.Add(new FieldState(entry.ConfigVar, row, field));
            return row;
        }

        private static bool GroupMatchesSearch(string groupName, string searchContext)
        {
            return !string.IsNullOrWhiteSpace(searchContext) && MatchesSearchContext(groupName, searchContext);
        }

        private static bool MatchesSearchContext(string s, string searchContext)
        {
            return !string.IsNullOrEmpty(s) && s.IndexOf(searchContext, StringComparison.InvariantCultureIgnoreCase) >= 0;
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
            where T : unmanaged
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
            field.labelElement.AddToClassList(FieldLabelClassName);
            field.tooltip = configVar.Description;
            field.value = binding.Value;
            return field;
        }

        private static void UpdateState(VisualElement row, VisualElement field, ConfigVarAttribute configVar, bool isPlaying)
        {
            var isEnabled = !configVar.IsReadOnly || !isPlaying;
            field.SetEnabled(isEnabled);
            row.EnableInClassList(ReadOnlyClassName, !isEnabled);
        }

        private readonly struct FieldState
        {
            public FieldState(ConfigVarAttribute configVar, VisualElement row, VisualElement field)
            {
                this.ConfigVar = configVar;
                this.Row = row;
                this.Field = field;
            }

            public ConfigVarAttribute ConfigVar { get; }

            public VisualElement Row { get; }

            public VisualElement Field { get; }
        }

        private readonly struct ConfigVarEntry
        {
            public ConfigVarEntry(ConfigVarAttribute configVar, FieldInfo fieldInfo)
            {
                this.ConfigVar = configVar;
                this.FieldInfo = fieldInfo;
                this.GroupName = GetGroupName(configVar.Name);
            }

            public ConfigVarAttribute ConfigVar { get; }

            public FieldInfo FieldInfo { get; }

            public string GroupName { get; }

            public bool Matches(string searchContext)
            {
                return string.IsNullOrWhiteSpace(searchContext)
                       || MatchesSearchContext(this.ConfigVar.Name, searchContext)
                       || MatchesSearchContext(this.ConfigVar.Description, searchContext)
                       || MatchesSearchContext(this.GroupName, searchContext);
            }

            private static string GetGroupName(string configVarName)
            {
                var separator = configVarName.IndexOf('.');
                return separator <= 0 ? "ungrouped" : configVarName.Substring(0, separator);
            }
        }
    }
}
