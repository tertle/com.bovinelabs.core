// <copyright file="ConfigVarsWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Window for config vars. </summary>
    public class ConfigVarsWindow : SettingsBaseWindow<ConfigVarsWindow>
    {
        private readonly Dictionary<string, ConfigVarPanel> panels = new();

        /// <inheritdoc />
        protected override string TitleText => "ConfigVars";

        protected override bool HideToggleShowEmpty => true;

        [MenuItem("BovineLabs/ConfigVars", priority = -31)]
        internal static void OpenSettings()
        {
            Open();
        }

        /// <inheritdoc />
        protected override void GetPanels(List<ISettingsPanel> settingPanels)
        {
            ConfigVarManager.Init();

            foreach (var p in this.panels)
            {
                p.Value.OnDeactivate();
            }

            this.panels.Clear();

            var configVars = ConfigVarManager.FindAllConfigVars();

            foreach (var (configVar, field) in configVars)
            {
                var key = configVar.Name.Split('.');
                var menu = key.Length < 2 ? "[null]" : key[0];

                if (!this.panels.TryGetValue(menu, out var panel))
                {
                    panel = this.panels[menu] = new ConfigVarPanel(menu);
                    settingPanels.Add(panel);
                }

                panel.ConfigVars.Add((configVar, field));
            }

            foreach (var p in this.panels)
            {
                p.Value.ConfigVars.Sort((p1, p2) => string.Compare(p1.ConfigVar.Name, p2.ConfigVar.Name, StringComparison.Ordinal));
            }
        }

        /// <inheritdoc />
        protected override void InitializeToolbar(VisualElement rootElement)
        {
            var resetButton = new ToolbarButton(this.ResetToDefault) { text = "Reset To Default" };
            rootElement.Add(resetButton);
        }

        private void ResetToDefault()
        {
            if (EditorUtility.DisplayDialog("Confirm Reset To Default", "Reset all config vars to default values?", "Reset", "Cancel"))
            {
                foreach (var panel in this.panels)
                {
                    foreach (var configVar in panel.Value.ConfigVars)
                    {
                        EditorPrefs.DeleteKey(configVar.ConfigVar.Name);
                    }
                }

                // TODO REFRESG
            }
        }
    }
}
