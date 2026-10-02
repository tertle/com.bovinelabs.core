namespace BovineLabs.Core.Editor
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Entities.Editor;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [Configurable]
    internal static partial class InspectorSearch
    {
        public const string Key = "core.inspector-search.enabled";

        private const string SearchClass = "bl-gameobject-inspector__search-field";
        [NoAutoStaticsCleanup]
        [ConfigVar(Key, true, "Enable the search button in the inspector")]
        private static readonly SharedStatic<bool> IsEnabled = SharedStatic<bool>.GetOrCreate<IsEnabledType>();

        [NoAutoStaticsCleanup]
        private static readonly PropertyInfo PropertyViewer = typeof(Editor).GetProperty(
            "propertyViewer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Inspector UI survives play-mode transitions; weak keys let replaced headers and their search fields be collected.
        [NoAutoStaticsCleanup]
        private static readonly ConditionalWeakTable<VisualElement, InspectorSearchField> SearchFields = new();

        [OnCodeInitializing]
        private static void Initialize()
        {
            if (!IsEnabled.Data)
            {
                return;
            }

            Editor.finishedDefaultHeaderGUI += Setup;
        }

        private static void Setup(Editor editor)
        {
            if (editor.GetType().Name != "GameObjectInspector" || PropertyViewer.GetValue(editor) is not EditorWindow window)
            {
                return;
            }

            var inspector = window.rootVisualElement.Q(className: "game-object-inspector");
            // Unity can measure the header before its EditorElement is added to the window's visual tree.
            if (inspector == null)
            {
                return;
            }

            SearchFields.GetValue(inspector, static element => new InspectorSearchField(element)).Refresh();
        }

        private sealed class InspectorSearchField : VisualElement
        {
            private readonly VisualElement _inspector;
            private readonly SearchElement _search;
            private readonly IVisualElementScheduledItem _refresh;

            public InspectorSearchField(VisualElement inspector)
            {
                _inspector = inspector;
                style.paddingBottom = 3;
                style.paddingTop = 3;
                style.paddingLeft = 3;
                style.paddingRight = 3;

                _search = new SearchElement { SearchDelay = 0 };
                _search.GetType().GetProperty("MaxFrameTime", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_search, 5);
                _search.GetType().GetProperty("HandlerType", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_search, "async");
                _search.GetType().GetProperty("SearchData", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_search, "Path");
                _search.AddToClassList(SearchClass);
                _search.RegisterSearchQueryHandler<VisualElement>(_ => ApplySearch());
                Add(_search);

                // Defer visual-tree changes until the header's IMGUI drawing pass has finished.
                _refresh = inspector.schedule.Execute(() =>
                {
                    if (parent == null)
                    {
                        var content = inspector.Q<InspectorElement>();
                        if (content == null)
                        {
                            return;
                        }

                        content.Add(this);
                    }

                    if (!string.IsNullOrEmpty(_search.value))
                    {
                        ApplySearch();
                    }
                });
            }

            public void Refresh()
            {
                if (parent == null || !string.IsNullOrEmpty(_search.value))
                {
                    _refresh.ExecuteLater(0);
                }
            }

            private void ApplySearch()
            {
                var search = _search.value.Trim();
                foreach (var component in _inspector.parent.Children())
                {
                    var nameSplit = component.name.Split('_');

                    if (nameSplit.Length != 3)
                    {
                        if (nameSplit.Length == 1 && nameSplit[0] == "RemainingPrefabComponentElement")
                        {
                            ElementUtility.SetVisible(component, false);
                        }

                        continue;
                    }

                    var n = nameSplit[1];

                    if (n is "GameObject" or "PrefabImporter")
                    {
                        continue;
                    }

                    var v = string.IsNullOrEmpty(search) || n.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  n.ToSentence().Contains(search, StringComparison.OrdinalIgnoreCase);
                    ElementUtility.SetVisible(component, v);
                }
            }
        }

        private struct IsEnabledType
        {
        }
    }
}
