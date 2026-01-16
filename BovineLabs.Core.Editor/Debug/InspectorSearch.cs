// <copyright file="InspectorSearch.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using System;
    using System.Reflection;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    using Resources = UnityEngine.Resources;

    [Configurable]
    internal static class InspectorSearch
    {
        public const string Key = "core.inspector-search.enabled";

        private const string SearchClass = "bl-gameobject-inspector__search-field";
        [ConfigVar(Key, true, "Enable the search button in the inspector")]
        private static readonly SharedStatic<bool> IsEnabled = SharedStatic<bool>.GetOrCreate<IsEnabledType>();

        private static Type inspectorWindowType = null!;

        internal static void Initialize()
        {
            if (!IsEnabled.Data)
            {
                return;
            }

            inspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

            // Selection.selectionChanged is nicer here, but it fails on domain reload to set itself up
            if (Selection.activeObject)
            {
                EditorApplication.update += Setup;
            }
            else
            {
                Selection.selectionChanged += SelectionChanged;
            }
        }

        private static void Setup()
        {
            var windows = Resources.FindObjectsOfTypeAll(inspectorWindowType);

            // If no windows skip and just wait for selection changes
            var any = windows.Length == 0;
            foreach (var w in windows)
            {
                any |= Setup((EditorWindow)w);
            }

            if (any)
            {
                EditorApplication.update -= Setup;
                Selection.selectionChanged += SelectionChanged;
            }
        }

        private static void SelectionChanged()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll(inspectorWindowType))
            {
                Setup((EditorWindow)w);
            }
        }

        private static bool Setup(EditorWindow w)
        {
            if (w.rootVisualElement == null)
            {
                return false;
            }

            var goInspector = w.rootVisualElement.Q<VisualElement>(className: "game-object-inspector");
            var inspector = goInspector?.Q<InspectorElement>();
            if (inspector == null)
            {
                return false;
            }

            var editor = inspector.GetType().GetField("m_Editor", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(inspector);
            if (editor.GetType().Name != "GameObjectInspector")
            {
                return true;
            }

            var se = inspector.Q<SearchElement>(className: SearchClass);
            if (se != null)
            {
                return true;
            }

            // Matching entities view style
            var sfp = new VisualElement
            {
                style =
                {
                    paddingBottom = 3,
                    paddingTop = 3,
                    paddingLeft = 3,
                    paddingRight = 3,
                },
            };

            se = new SearchElement { SearchDelay = 0 };
            se.GetType().GetProperty("MaxFrameTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(se, 5);
            se.GetType().GetProperty("HandlerType", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(se, "async");
            se.GetType().GetProperty("SearchData", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(se, "Path");
            se.AddToClassList(SearchClass);
            se.RegisterSearchQueryHandler<VisualElement>(query =>
            {
                var s = query.SearchString.Trim().ToLower();
                var root = w.rootVisualElement.Q<VisualElement>(className: "unity-inspector-editors-list");
                foreach (var c in root.Children())
                {
                    var nameSplit = c.name.Split("_");

                    if (nameSplit.Length != 3)
                    {
                        if (nameSplit.Length == 1 && nameSplit[0] == "RemainingPrefabComponentElement")
                        {
                            ElementUtility.SetVisible(c, false);
                            continue;
                        }

                        BLGlobalLogger.LogInfoString($"{c.name} unknown format");
                        continue;
                    }

                    var n = nameSplit[1];

                    if (n is "GameObject" or "PrefabImporter")
                    {
                        continue;
                    }

                    var visible = string.IsNullOrEmpty(s) || n.ToLower().Contains(s) || n.ToSentence().ToLower().Contains(s);
                    ElementUtility.SetVisible(c, visible);
                }
            });

            sfp.Add(se);
            inspector.Add(sfp);
            return true;
        }

        private struct IsEnabledType
        {
        }
    }
}
