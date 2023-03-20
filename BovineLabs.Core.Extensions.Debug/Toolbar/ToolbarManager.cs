// <copyright file="ToolbarManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Debug.Toolbar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.UI;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The system for the ribbon toolbar. </summary>
    internal static class ToolbarManager
    {
        private const string RootName = "root";
        private const string MenuName = "menu";
        private const string ShowButtonName = "show";

        private const string ContentsClass = "bl-toolbar-contents";
        private const string ButtonHighlightClass = "bl-toolbar-highlight";
        private const string ToolbarGroupNameClass = "group-name";
        private const string ToolbarGroupClass = "bl-toolbar-group";

        private static readonly Dictionary<string, ToolbarTab> ToolbarTabs = new();

        private static TemplateContainer panelElement;

        private static (string Name, ToolbarTab Tab) activeTab;

        private static VisualElement menuElement = new();
        private static VisualElement rootElement;

        private static float uiHeight;
        private static Button showButton;
        private static bool showRibbon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            // TODO
            var asset = Resources.Load<VisualTreeAsset>("Toolbar");

            panelElement = asset.CloneTree();
            panelElement.name = "Toolbar";

            OnLoad(panelElement);

            UIDocumentManager.Instance.AddRoot(panelElement, -1000);
#if UNITY_EDITOR
            UIDocumentManager.Instance.EditorRebuild += () =>
            {
                UIDocumentManager.Instance.RemoveRoot(panelElement);
                UIDocumentManager.Instance.AddRoot(panelElement, -1000);
            };
#endif

            default(EntityCommandBuffer).DestroyEntity(default(EntityQuery));

            UIDocumentManager.Instance.Root.RegisterCallback<GeometryChangedEvent>(OnRootContentChanged);
        }

        public static void AddGroup(string tabName, ToolbarTab.Group group)
        {
            if (!ToolbarTabs.TryGetValue(tabName, out var tabs))
            {
                tabs = ToolbarTabs[tabName] = CreateTab(tabName);
            }

            var parent = new VisualElement();
            parent.AddToClassList(ToolbarGroupClass);
            parent.Add(group.RootElement);

            var groupLabel = new Label(group.Name);
            groupLabel.AddToClassList(ToolbarGroupNameClass);

            parent.Add(groupLabel);

            tabs.Groups.Add((group, parent));
            tabs.Groups.Sort((t1, t2) => string.Compare(t1.Group.Name, t2.Group.Name, StringComparison.Ordinal));

            var index = tabs.Groups.FindIndex(g => g.Group == group);
            tabs.Parent.Insert(index, parent);

            // If the first toolbar group loads after the toolbar we want to set it as default
            if (rootElement != null && activeTab == default)
            {
                SetToolbarActive(tabName);
            }
        }

        public static void RemoveGroup(string tabName, ToolbarTab.Group group)
        {
            if (!ToolbarTabs.ContainsKey(tabName))
            {
                return;
            }

            var tabs = ToolbarTabs[tabName];
            var index = tabs.Groups.FindIndex(g => g.Group == group);
            if (index == -1)
            {
                return;
            }

            tabs.Parent.Remove(tabs.Groups[index].Parent);
            tabs.Groups.RemoveAt(index);

            // Removed all groups, remove the tab
            if (tabs.Groups.Count == 0)
            {
                DestroyTab(tabName);
            }
        }

        public static bool IsTabVisible(string tabName)
        {
            return activeTab.Name == tabName;
        }

        private static void OnLoad(VisualElement panel)
        {
            var oldMenuItem = menuElement;
            menuElement = panel.Q<VisualElement>(MenuName);

            // Move any elements that were added to the temp menu element
            while (oldMenuItem.childCount > 0)
            {
                var child = oldMenuItem[0];
                menuElement.Add(child);
            }

            showButton = panel.Q<Button>(ShowButtonName);
            showButton.clicked += ShowToggle;

            rootElement = panel.Q<VisualElement>(RootName);
            rootElement.RegisterCallback<GeometryChangedEvent>(OnRootElementGeometryChanged);

            uiHeight = Screen.height;

            ShowRibbon(showRibbon);
            SetDefaultGroup();
        }

        private static ToolbarTab CreateTab(string tabName)
        {
            var button = new Button { text = tabName };
            button.clicked += () => SetToolbarActive(tabName);
            menuElement.Add(button);

            var contents = new VisualElement();
            contents.AddToClassList(ContentsClass);

            var toolbarTab = new ToolbarTab(button, contents);
            return toolbarTab;
        }

        private static void DestroyTab(string tabName)
        {
            var tab = ToolbarTabs[tabName];
            menuElement.Remove(tab.Button);
            ToolbarTabs.Remove(tabName);

            if (activeTab.Name == tabName)
            {
                SetDefaultGroup();
            }
        }

        private static void SetDefaultGroup()
        {
            SetToolbarActive(string.Empty);

            // First is always the toggle button
            if (menuElement.childCount == 0)
            {
                return;
            }

            var firstButton = (Button)menuElement.contentContainer.Children().First();
            var group = ToolbarTabs.First(g => g.Value.Button == firstButton);
            SetToolbarActive(group.Key);
        }

        private static void SetToolbarActive(string name)
        {
            if (name == activeTab.Name)
            {
                ShowRibbon(true);
                return;
            }

            if (activeTab != default)
            {
                activeTab.Tab.Button.RemoveFromClassList(ButtonHighlightClass);

                // something else has already removed it or moved it
                if (activeTab.Tab.Parent.parent == rootElement)
                {
                    rootElement.Remove(activeTab.Tab.Parent);
                }

                activeTab = default;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!ToolbarTabs.TryGetValue(name, out var tab))
            {
                throw new ArgumentException("Unknown button", nameof(name));
            }

            activeTab = (name, tab);
            tab.Button.AddToClassList(ButtonHighlightClass);

            rootElement.Add(activeTab.Tab.Parent);
            ShowRibbon(true);
        }

        private static void ShowRibbon(bool show)
        {
            if (show)
            {
                if (activeTab != default)
                {
                    rootElement.Add(activeTab.Tab.Parent);
                }
            }
            else
            {
                if (activeTab != default)
                {
                    rootElement.Remove(activeTab.Tab.Parent);
                }
            }

            showRibbon = show;
        }

        private static void ShowToggle()
        {
            ShowRibbon(!showRibbon);
        }

        private static void OnRootContentChanged(GeometryChangedEvent evt)
        {
            var height = UIDocumentManager.Instance.Root.contentRect.height;

            if (math.abs(uiHeight - height) > float.Epsilon)
            {
                uiHeight = height;
                ResizeViewRect(rootElement.contentRect);
            }
        }

        private static void OnRootElementGeometryChanged(GeometryChangedEvent evt)
        {
            ResizeViewRect(evt.newRect);
        }

        private static void ResizeViewRect(Rect uiRect)
        {
            if (uiHeight == 0)
            {
                return;
            }

            var cameraHeightNormalized = (uiHeight - uiRect.height) / uiHeight;

            foreach (var world in WorldUtility.AllExcludingAdvanced())
            {
                world.EntityManager.CompleteAllTrackedJobs();
                if (world.EntityManager.TryGetSingletonEntity<Camera>(out var cameraEntity))
                {
                    var camera = world.EntityManager.GetComponentObject<Camera>(cameraEntity);
                    var rect = camera.rect;
                    rect.height = cameraHeightNormalized;
                    camera.rect = rect;
                    break;
                }
            }
        }
    }
}
