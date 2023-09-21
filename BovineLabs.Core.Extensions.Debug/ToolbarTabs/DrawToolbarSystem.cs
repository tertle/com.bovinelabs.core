// <copyright file="DrawToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && BL_DRAW
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Debug.Toolbar;
    using BovineLabs.Core.UI;
    using BovineLabs.Draw;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial class DrawToolbarSystem : ToolbarSystemBase
    {
        private readonly List<string> categories = new();

        private readonly List<string> categoriesBuffer = new();
        private readonly List<int> systems = new();
        private readonly List<int> systemsBuffer = new();

        private VisualTreeAsset? asset;
        private PopupFlagField? categoryElement;
        private Toggle? enabled;

        private bool isEnabled;
        private int knownLength;
        private DrawSystem.Singleton singleton;
        private PopupFlagField? systemElement;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Name => "Drawer";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            this.asset = Resources.Load<VisualTreeAsset>("DrawGroup");
        }

        protected override void OnStartRunningSystem()
        {
            this.singleton = SystemAPI.GetSingleton<DrawSystem.Singleton>();
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
            this.enabled = element.Q<Toggle>("Enable");
            this.enabled.RegisterValueChangedCallback(this.EnableOnChanged);

            // UpdateToggleText(this.enabled, IsEnabled.Data);

            this.categoryElement = element.Q<PopupFlagField>("Categories");
            this.categoryElement.SetDisplayNames(Enumerable.Empty<string>());
            this.categoryElement.RegisterValueChangedCallback(this.CategoryFilterOnChanged);

            this.systemElement = element.Q<PopupFlagField>("Systems");
            this.systemElement.SetDisplayNames(Enumerable.Empty<string>());
            this.systemElement.RegisterValueChangedCallback(this.SystemFilterOnChanged);

            // set default values;
            // this.CategoryFilterOnChanged(CategoryFilter.Data.Value);
            // this.SystemFilterOnChanged(SystemFilter.Data.Value);
        }

        /// <inheritdoc />
        protected override void OnUpdateVisible()
        {
            this.UpdateEnable();
            this.UpdateSystems();
            this.UpdateCategory();
        }

        private void EnableOnChanged(ChangeEvent<bool> evt)
        {
            this.singleton.IsEnabled.Value = evt.newValue;
        }

        private static IEnumerable<string> GetDisplayNames(IEnumerable<int> types)
        {
            return types.Select(s => TypeManager.GetSystemName(s).ToString()).Select(s =>
            {
                // Remove any namespaces
                var index = s.LastIndexOf('.') + 1;
                return index == 0 ? s : s.Substring(index, s.Length - index);
            });
        }

        private static void UpdateToggleText(Toggle toggle, bool b)
        {
            toggle.text = b ? "Enabled" : "Disabled";
            toggle.SetValueWithoutNotify(b);
        }

        private void UpdateEnable()
        {
            if (this.isEnabled == this.singleton.IsEnabled.Value)
            {
                return;
            }

            this.isEnabled = !this.isEnabled;
            this.IsEnabledOnChanged(this.isEnabled);
        }

        private void UpdateSystems()
        {
            this.systemsBuffer.Clear();
            using var se = this.singleton.KnownSystemSet.GetEnumerator();
            while (se.MoveNext())
            {
                this.systemsBuffer.Add(se.Current);
            }

            if (this.systemsBuffer.SequenceEqual(this.systems))
            {
                return;
            }

            this.systems.Clear();
            this.systems.AddRange(this.systemsBuffer);
            this.systemElement!.SetDisplayNames(GetDisplayNames(this.systems));
        }

        private void UpdateCategory()
        {
            this.categoriesBuffer.Clear();
            using var ce = this.singleton.KnownCategorySet.GetEnumerator();
            while (ce.MoveNext())
            {
                this.categoriesBuffer.Add(ce.Current.ToString());
            }

            if (this.categoriesBuffer.SequenceEqual(this.categories))
            {
                return;
            }

            this.categories.Clear();
            this.categories.AddRange(this.categoriesBuffer);
            this.categoryElement!.SetDisplayNames(this.categories);
        }

        private void CategoryFilterOnChanged(ChangeEvent<IReadOnlyList<int>> evt)
        {
            if (evt.newValue == null)
            {
                return;
            }

            this.singleton.CategoryFilterSet.Clear();
            foreach (var category in evt.newValue.Select(e => this.categories[e]))
            {
                this.singleton.CategoryFilterSet.Add(category);
            }
        }

        private void SystemFilterOnChanged(ChangeEvent<IReadOnlyList<int>> evt)
        {
            if (evt.newValue == null)
            {
                return;
            }

            this.singleton.SystemFilterSet.Clear();
            foreach (var system in evt.newValue.Select(e => this.systems[e]))
            {
                this.singleton.SystemFilterSet.Add(system);
            }
        }

        private void IsEnabledOnChanged(bool e)
        {
            this.enabled!.SetValueWithoutNotify(e);
            UpdateToggleText(this.enabled, e);
        }
    }
}
#endif
