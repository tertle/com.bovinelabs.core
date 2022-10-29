// <copyright file="CustomHierarchy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Hierarchy
{
    using System;
    using System.Reflection;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [InitializeOnLoad]
    public static class CustomHierarchy
    {
        private const string DefaultKey = "bl-hierarchy-";
        private const string AllKey = DefaultKey + "all";
        private const string PrefabKey = DefaultKey + "prefab";
        private const string DisabledKey = DefaultKey + "disabled";
        private const string SystemsKey = DefaultKey + "systems";

        private static HierarchyWindow currentWindow;

        static CustomHierarchy()
        {
            EditorApplication.update += Update;
        }

        private static bool AllQuery
        {
            get => EditorPrefs.GetBool(AllKey, false);
            set => EditorPrefs.SetBool(AllKey, value);
        }

        private static bool IncludePrefabs
        {
            get => EditorPrefs.GetBool(PrefabKey, true);
            set => EditorPrefs.SetBool(PrefabKey, value);
        }

        private static bool IncludeDisabled
        {
            get => EditorPrefs.GetBool(DisabledKey, true);
            set => EditorPrefs.SetBool(DisabledKey, value);
        }

        private static bool IncludeSystems
        {
            get => EditorPrefs.GetBool(SystemsKey, false);
            set => EditorPrefs.SetBool(SystemsKey, value);
        }

        private static void Update()
        {
            if (currentWindow == null)
            {
                if (EditorWindow.HasOpenInstances<HierarchyWindow>())
                {
                    currentWindow = EditorWindow.GetWindow<HierarchyWindow>();
                    Setup(currentWindow);
                }
            }
        }

        private static void Setup(HierarchyWindow hierarchyWindow)
        {
            var hierarchyField = typeof(HierarchyWindow).GetField("m_Hierarchy", BindingFlags.Instance | BindingFlags.NonPublic);
            var hierarchy = (Hierarchy)hierarchyField!.GetValue(hierarchyWindow);

            var filterField = typeof(Hierarchy).GetField("m_Filter", BindingFlags.Instance | BindingFlags.NonPublic);

            var searchElement = hierarchyWindow.rootVisualElement.Q<SearchElement>();
            searchElement.RegisterSearchQueryHandler<HierarchyNodeHandle>(_ =>
            {
                var filter = (HierarchyFilter)filterField!.GetValue(hierarchy);

                if (filter == null || filter.FilterQueryDesc == null)
                {
                    return;
                }

                // Swap our any and all queries
                var desc = filter.FilterQueryDesc;
                if (AllQuery)
                {
                    desc.All = desc.Any;
                    desc.Any = Array.Empty<ComponentType>();
                }

                var options = EntityQueryOptions.Default;
                if (IncludePrefabs)
                {
                    options |= EntityQueryOptions.IncludePrefab;
                }

                if (IncludeDisabled)
                {
                    options |= EntityQueryOptions.IncludeDisabledEntities;
                }

                if (IncludeSystems)
                {
                    options |= EntityQueryOptions.IncludeSystems;
                }

                desc.Options = options;
            });

            // Remove the existing delegate events so we can replace the subscription with our own
            var filterButton = hierarchyWindow.rootVisualElement.Q<Button>("search-element-add-filter-button");
            var clickedEvent = typeof(Clickable).GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic);
            var events = (MulticastDelegate)clickedEvent!.GetValue(filterButton.clickable);
            foreach (var action in events.GetInvocationList())
            {
                filterButton.clickable.clicked -= (Action)action;
            }

            filterButton.clickable.clicked += () =>
            {
                var filterDropdown = new FilterPopupElement(searchElement.FilterPopupWidth);

                filterDropdown.AddPopupItem("Use All", AllQuery, "Use ALL instead of ANY in the query", () => AllQuery = !AllQuery);
                filterDropdown.AddPopupItem("Include Prefabs", IncludePrefabs, "Include prefabs in the query", () => IncludePrefabs = !IncludePrefabs);
                filterDropdown.AddPopupItem("Include Disabled", IncludeDisabled, "Include disabled in the query", () => IncludeDisabled = !IncludeDisabled);
                filterDropdown.AddPopupItem("Include Systems", IncludeSystems, "Include systems in the query", () => IncludeSystems = !IncludeSystems);

                filterDropdown.ShowAtPosition(filterButton.worldBound);
            };
        }

        // Based off SearchElement.FilterPopupElement
        private class FilterPopupElement : PopupElement
        {
            private const int HeaderHeight = 28;
            private const int ElementHeight = 16;

            private readonly float width;
            private readonly VisualElement choices;

            private int elementCount;

            public FilterPopupElement(int width)
            {
                this.width = width;

                Unity.Entities.Editor.Resources.Templates.Variables.AddStyles(this);
                Unity.Entities.Editor.Resources.Templates.SearchElementFilterPopup.Clone(this);
                this.AddToClassList(UssClasses.SearchElementFilterPopup.Root);

                this.Q<Label>().text = "Filter Options";
                this.choices = this.Q<VisualElement>("search-element-filter-popup-choices");
            }

            public void AddPopupItem(string filterText, bool state, string filterTooltip = "", Action action = null)
            {
                this.elementCount++;

                // Create a button with two labels.
                var choiceButton = new Button { tooltip = filterTooltip };
                var nameLabel = new Label(filterText);
                var tokenLabel = new Label(state ? "true" : "false");

                // Setup uss classes.
                choiceButton.ClearClassList();
                choiceButton.AddToClassList(UssClasses.SearchElementFilterPopup.ChoiceButton);
                nameLabel.AddToClassList(UssClasses.SearchElementFilterPopup.ChoiceName);
                tokenLabel.AddToClassList(UssClasses.SearchElementFilterPopup.ChoiceToken);

                // Setup visual tree.
                choiceButton.Add(nameLabel);
                choiceButton.Add(tokenLabel);
                this.choices.Add(choiceButton);

                // Setup event handlers.
                choiceButton.clickable.clicked += () =>
                {
                    // Close the window.
                    action?.Invoke();
                    this.Close();
                };
            }

            protected override Vector2 GetSize() => new(this.width, HeaderHeight + (ElementHeight * this.elementCount));
        }
    }
}
