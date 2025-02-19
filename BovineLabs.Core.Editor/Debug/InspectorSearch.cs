// <copyright file="InspectorSearch.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INSPECTOR_SEARCH
namespace BovineLabs.Core.Editor
{
    using System;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Extensions;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Resources = UnityEngine.Resources;

    [InitializeOnLoad]
    public static class InspectorSearch
    {
        private const string SearchClass = "bl-gameobject-inspector__search-field";
        private static readonly Type InspectorWindowType;

        static InspectorSearch()
        {
            InspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

            // Selection.selectionChanged is nicer here, but it fails on domain reload to set itself up
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            var windows = Resources.FindObjectsOfTypeAll(InspectorWindowType);
            var all = windows.Length > 0;
            foreach (var w in windows)
            {
                all &= Setup((EditorWindow)w);
            }

            if (all)
            {
                EditorApplication.update -= Initialize;
                Selection.selectionChanged += SelectionChanged;
            }
        }

        private static void SelectionChanged()
        {
            var inspectorWindow = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

            foreach (var w in Resources.FindObjectsOfTypeAll(inspectorWindow))
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
                        Debug.Log($"{c.name} unknown format");
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
    }
}
#endif
