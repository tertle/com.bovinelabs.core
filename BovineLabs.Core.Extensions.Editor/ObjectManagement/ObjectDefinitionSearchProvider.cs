// <copyright file="ObjectDefinitionSearchProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Authoring.ObjectManagement;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Editor.Bridge;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEditor.ShortcutManagement;
    using UnityEngine;

    public static class ObjectDefinitionSearchProvider
    {
        private const string Type = "objectdefinition";

        private static QueryEngine<ObjectDefinitionDescriptor>? queryEngine;

        private static QueryEngine<ObjectDefinitionDescriptor> QueryEngine => queryEngine ??= SetupQueryEngine();

        [SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            return new SearchProvider(Type, "Object Definitions")
            {
                filterId = "od:",
                isExplicitProvider = true,
                active = true,
                showDetails = true,
                fetchItems = FetchItems,
                fetchPropositions = FetchPropositions,
                trackSelection = SelectItem,
                toObject = (item, _) => ((ObjectDefinitionDescriptor)item.data).ObjectDefinition,
            };
        }

        private static void SelectItem(SearchItem item, SearchContext ctx)
        {
            var data = (ObjectDefinitionDescriptor)item.data;
            EditorGUIUtility.PingObject((Object)data.ObjectDefinition);
            Selection.activeObject = data.ObjectDefinition;
        }

        [MenuItem("Window/Search/Object Definition", priority = 1391)]
        private static void OpenProviderMenu()
        {
            OpenProvider();
        }

        [Shortcut("Help/Quick Search/Assets")]
        private static void PopQuickSearch()
        {
            // Open Search with only the "Asset" provider enabled.
            OpenProvider();
        }

        private static void OpenProvider()
        {
            SearchService.ShowContextual(Type);
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;

            ParsedQuery<ObjectDefinitionDescriptor>? query = null;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = QueryEngine.ParseQuery(context.searchQuery);
                if (!query.valid)
                {
                    query = null;
                }
            }

            var score = 0;
            var guids = AssetDatabase.FindAssets("t:ObjectDefinition");

            var toFilter = new ObjectDefinitionDescriptor[1];

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ObjectDefinition>(path);

                toFilter[0] = new ObjectDefinitionDescriptor(asset);

                foreach (var data in query?.Apply(toFilter) ?? toFilter)
                {
                    yield return provider.CreateItem(context, data.ID, score++, data.Name, data.Description, null, data);
                }
            }
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            foreach (var p in SearchBridge.GetPropositions(QueryEngine))
            {
                yield return p;
            }

            foreach (var l in SearchBridge.GetPropositionsFromListBlockType(typeof(QueryCategoryTypeBlock)))
            {
                yield return l;
            }
        }

        private static QueryEngine<ObjectDefinitionDescriptor> SetupQueryEngine()
        {
            var query = new QueryEngine<ObjectDefinitionDescriptor>();
            query.SetSearchDataCallback(GetWords);

            SearchBridge
                .SetFilter(query, "n", data => data.Name, new[] { "=", ":" })
                .AddOrUpdateProposition(category: null, label: "Name", replacement: "n:Name", help: "Search Entry by Object Definition Name");

            SearchBridge
                .SetFilter(query, "d", data => data.Description, new[] { "=", ":" })
                .AddOrUpdateProposition(category: null, label: "Description", replacement: "d:Description",
                    help: "Search Entry by Object Definition Description");

            SearchBridge.AddFilter<string, ObjectDefinitionDescriptor>(query, "ca", OnTypeFilter, new[] { "=" });

            return query;
        }

        private static bool OnTypeFilter(ObjectDefinitionDescriptor descriptor, QueryFilterOperator op, string value)
        {
            var results = descriptor.Categories;
            return SearchBridge.CompareWords(op, value, results);
        }

        private static IEnumerable<string> GetWords(ObjectDefinitionDescriptor desc)
        {
            yield return desc.Name;
            yield return desc.Description;

            foreach (var c in desc.Categories)
            {
                yield return c;
            }
        }

        private readonly struct ObjectDefinitionDescriptor
        {
            public readonly ObjectDefinition ObjectDefinition;
            private static Dictionary<int, string>? map;

            public ObjectDefinitionDescriptor(ObjectDefinition objectDefinition)
            {
                map ??= EditorSettingsUtility.GetSettings<ObjectCategories>().Keys.ToDictionary(k => k.Value, k => k.Name);

                this.ObjectDefinition = objectDefinition;

                this.Categories = new List<string>();
                var c = (uint)objectDefinition.Categories;
                while (c != 0)
                {
                    var index = math.tzcnt(c);
                    var shifted = (uint)(1 << index);

                    if (map.TryGetValue(index, out var n))
                    {
                        this.Categories.Add(n);
                    }

                    this.Categories.Add(shifted.ToString());

                    c ^= shifted;
                }
            }

            public string ID => this.ObjectDefinition.ID.ToString();

            public string Name => this.ObjectDefinition.FriendlyName;

            public string Description => this.ObjectDefinition.Description;

            public List<string> Categories { get; }
        }

        [QueryListBlock("Category", "ca", "ca")]
        private class QueryCategoryTypeBlock : QueryListBlock
        {
            public QueryCategoryTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
                : base(source, id, value, attr)
            {
            }

            public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
            {
                var c = flags.HasFlag(SearchPropositionFlags.NoCategory) ? null : this.category;
                var categories = EditorSettingsUtility.GetSettings<ObjectCategories>();

                foreach (var ca in categories.Keys)
                {
                    var n = ca.Name.Contains(" ") ? $"\"{ca.Name}\"" : ca.Name;
                    yield return new SearchProposition(c, ca.Name, n, type: this.GetType(), data: n);
                }
            }
        }
    }
}
#endif
