// <copyright file="SettingsWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEngine;

    /// <summary> The settings editor window. </summary>
    internal class SettingsWindow : SettingsBaseWindow<SettingsWindow>
    {
        private readonly Dictionary<Type, Type> settingsPanelMap = new();

        /// <inheritdoc />
        protected override string TitleText => "Settings";

        [MenuItem(EditorMenus.RootMenu + "Settings", priority = -30)]
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
                }
                catch (ArgumentException)
                {
                    BLGlobalLogger.LogErrorString($"Multiple panels found for {settings}");
                }
            }

            // Get all custom panel implementations
            foreach (var settingsType in ReflectionUtility.GetAllImplementationsRootOnly<ISettings, ScriptableObject>())
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
                    panel = (ISettingsPanel)Activator.CreateInstance(s.Value);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);

                    continue;
                }

                settingPanels.Add(panel);
            }
        }

        private static IEnumerable<(Type Settings, Type Panel)> GetAllSettingsBasePanels()
        {
            return from t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
                where !t.IsAbstract && !t.IsInterface && !t.IsGenericType
                let i = t.BaseType
                where i is { IsGenericType: true } && i.GetGenericTypeDefinition() == typeof(SettingsBasePanel<>)
                select (i.GetGenericArguments()[0], t);
        }
    }
}
