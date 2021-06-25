// <copyright file="SettingsWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Basics.Helpers;
    using BovineLabs.Basics.Settings;
    using UnityEditor;
    using UnityEngine;

    /// <summary> The settings editor window. </summary>
    internal class SettingsWindow : SettingsBaseWindow<SettingsWindow>
    {
        private readonly Dictionary<Type, Type> settingsPanelMap = new Dictionary<Type, Type>();

        /// <inheritdoc/>
        protected override string TitleText { get; } = "Settings";

        [MenuItem("BovineLabs/Settings", priority = 11)]
        internal static void OpenSettings()
        {
            Open();
        }

        protected override void GetPanels(List<ISettingsPanel> settingPanels)
        {
            this.settingsPanelMap.Clear();

            if (EditorApplication.isCompiling)
            {
                return;
            }

            foreach (var (settings, panel) in GetAllSettingsBasePanels())
            {
                try
                {
                    this.settingsPanelMap.Add(settings, panel);

                    Debug.Log($"Custom {settings}, {panel}");
                }
                catch (ArgumentException)
                {
                    Debug.LogError($"Multiple panels found for {settings}");
                }
            }

            // Get all custom panel implementations
            foreach (var settingsType in ReflectionUtility.GetAllImplementations<ISettings, ScriptableObject>())
            {
                // Custom implementation
                if (this.settingsPanelMap.ContainsKey(settingsType))
                {
                    continue;
                }

                var genericPanelType = typeof(GenericSettingsPanel<>);
                var panelType = genericPanelType.MakeGenericType(settingsType);

                this.settingsPanelMap.Add(settingsType, panelType);
            }

            // Create an implementations for all settings without a custom implementation
            foreach (var s in this.settingsPanelMap)
            {
                ISettingsPanel panel;
                try
                {
                    panel = Activator.CreateInstance(s.Value) as ISettingsPanel;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    continue;
                }

                settingPanels.Add(panel);
            }

            settingPanels.Sort((p1, p2) => string.Compare(p1.DisplayName, p2.DisplayName, StringComparison.Ordinal));
        }

        private static IEnumerable<(Type, Type)> GetAllSettingsBasePanels()
        {
            return from t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                where !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                let i = t.BaseType
                where i != null && i.IsGenericType && i.GetGenericTypeDefinition() == typeof(SettingsBasePanel<>)
                select ValueTuple.Create(i.GetGenericArguments()[0], t);
        }
    }
}