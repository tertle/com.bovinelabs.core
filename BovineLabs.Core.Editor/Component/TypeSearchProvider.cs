// <copyright file="TypeSearchProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Editor.Bridge;
    using Unity.Entities;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEditor.ShortcutManagement;

    public static class TypeSearchProvider
    {
        [NoAutoStaticsCleanup]
        private static QueryEngine<TypeDescriptor> queryEngine;

        private static QueryEngine<TypeDescriptor> QueryEngine => queryEngine ??= SetupQueryEngine();

        [SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            return new SearchProvider(TypeAsset.SearchProviderType, "Types")
            {
                filterId = "at:",
                isExplicitProvider = true,
                active = true,
                showDetails = true,
                fetchItems = FetchItems,
                fetchPropositions = FetchPropositions,
            };
        }

        [MenuItem("Window/Search/Types", priority = 1391)]
        private static void OpenProviderMenu()
        {
            OpenProvider();
        }

        [Shortcut("Help/Quick Search/Types")]
        private static void PopQuickSearch()
        {
            OpenProvider();
        }

        private static void OpenProvider()
        {
            SearchService.ShowContextual(TypeAsset.SearchProviderType);
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;

            ParsedQuery<TypeDescriptor> query = null;

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = QueryEngine.ParseQuery(context.searchQuery);
                if (!query.valid)
                {
                    query = null;
                }
            }

            var toFilter = new TypeDescriptor[1];

            var score = 0;
            foreach (var descriptor in GetTypeDescriptors(searchQuery))
            {
                toFilter[0] = descriptor;

                foreach (var data in query?.Apply(toFilter) ?? toFilter)
                {
                    yield return provider.CreateItem(context, data.FullName, score++, data.Name, data.SimplifiedQualifiedName, null, data.FullName);
                }
            }
        }

        private static IEnumerable<TypeDescriptor> GetTypeDescriptors(string searchQuery)
        {
            if (UsesEcsFilters(searchQuery))
            {
                foreach (var typeInfo in TypeManager.AllTypes)
                {
                    if (typeInfo.Type != null)
                    {
                        yield return new TypeDescriptor(typeInfo);
                    }
                }

                yield break;
            }

            foreach (var type in ReflectionUtility.AllTypes)
            {
                yield return new TypeDescriptor(type);
            }
        }

        private static bool UsesEcsFilters(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                return false;
            }

            return ContainsFilter(searchQuery, "component") || ContainsFilter(searchQuery, "componentdata") ||
                ContainsFilter(searchQuery, "enableable") || ContainsFilter(searchQuery, "zerosized");
        }

        private static bool ContainsFilter(string searchQuery, string filter)
        {
            return searchQuery.IndexOf($"{filter}=", StringComparison.OrdinalIgnoreCase) >= 0 ||
                searchQuery.IndexOf($"{filter}:", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            foreach (var p in SearchBridge.GetPropositions(QueryEngine))
            {
                yield return p;
            }

            // foreach (var l in SearchBridge.GetPropositionsFromListBlockType(typeof(InheritTypeBlock)))
            // {
            //     yield return l;
            // }
        }

        private static QueryEngine<TypeDescriptor> SetupQueryEngine()
        {
            var query = new QueryEngine<TypeDescriptor>();
            query.SetSearchDataCallback(GetWords);

            SearchBridge.SetFilter(query, "unmanaged", data => data.IsUnmanaged)
                .AddOrUpdateProposition(category: null, label: "Is Unmanaged", replacement: "unmanaged=true", help: "Limit search to unmanaged types");

            SearchBridge.SetFilter(query, "unityobject", data => data.IsUnityObject)
                .AddOrUpdateProposition(category: null, label: "Is Unity Object", replacement: "unityobject=true", help: "Limit search to Unity Objects");

            SearchBridge.SetFilter(query, "component", data => data.IsComponent)
                .AddOrUpdateProposition(category: null, label: "Is ECS Component", replacement: "component=true",
                    help: "Limit search to component and buffer types");

            SearchBridge.SetFilter(query, "componentdata", data => data.IsComponentData)
                .AddOrUpdateProposition(category: null, label: "Is Component Data", replacement: "componentdata=true",
                    help: "Limit search to component data types");

            SearchBridge.SetFilter(query, "enableable", data => data.IsEnableable)
                .AddOrUpdateProposition(category: null, label: "Is Enableable", replacement: "enableable=true",
                    help: "Limit search to enableable component types");

            SearchBridge.SetFilter(query, "zerosized", data => data.IsZeroSized)
                .AddOrUpdateProposition(category: null, label: "Is Zero Sized", replacement: "zerosized=true",
                    help: "Limit search to zero-sized component types");

            SearchBridge.SetFilter(query, "editor", data => data.IsEditorAssembly)
                .AddOrUpdateProposition(category: null, label: "Is Editor Assembly", replacement: "editor=true",
                    help: "Limit search to types in editor assemblies");

            query.AddFilter<string>("inherit", OnInheritFilter, /*Transformer,*/ new[] { "=", ":" });
            // query.TryGetFilter("inherit", out var inherit);
            // inherit.AddOrUpdateProposition(category: null, label: "Inherits", replacement: "inherit:", help: "Search Entry by Inheritance");

            return query;
        }

        private static bool OnInheritFilter(TypeDescriptor descriptor, string operatorToken, string filterValue)
        {
            var type = Type.GetType(filterValue); // this is awful but i can't seem to figure it out
            return type != null && type.IsAssignableFrom(descriptor.Type);
        }

        private static IEnumerable<string> GetWords(TypeDescriptor desc)
        {
            yield return desc.Name;
        }

        private readonly struct TypeDescriptor
        {
            public readonly Type Type;

            private readonly TypeIndex typeIndex;
            private readonly TypeManager.TypeCategory category;
            private readonly bool isZeroSized;

            public TypeDescriptor(Type type)
            {
                this.Type = type;
                this.typeIndex = TypeIndex.Null;
                this.category = TypeManager.TypeCategory.UnityEngineObject;
                this.isZeroSized = false;
            }

            public TypeDescriptor(TypeManager.TypeInfo typeInfo)
            {
                this.Type = typeInfo.Type;
                this.typeIndex = typeInfo.TypeIndex;
                this.category = typeInfo.Category;
                this.isZeroSized = typeInfo.IsZeroSized;
            }

            public string Name => this.Type.Name;

            public string SimplifiedQualifiedName => $"{this.Type.FullName}, {this.Type.Assembly.GetName().Name}";

            public string FullName => this.Type.AssemblyQualifiedName;

            public bool IsUnmanaged => UnsafeUtility.IsUnmanaged(this.Type);

            public bool IsUnityObject => typeof(UnityEngine.Object).IsAssignableFrom(this.Type);

            public bool IsComponent => this.category is TypeManager.TypeCategory.ComponentData or TypeManager.TypeCategory.BufferData;

            public bool IsComponentData => this.category == TypeManager.TypeCategory.ComponentData;

            public bool IsEnableable => this.typeIndex != TypeIndex.Null && TypeManager.IsEnableable(this.typeIndex);

            public bool IsZeroSized => this.isZeroSized;

            public bool IsEditorAssembly => this.Type.Assembly.IsAssemblyEditorAssembly() || this.Type.Assembly.IsTestEditorAssembly();
        }
        //
        // [QueryListBlock("Inherit", "inherit", "inherit")]
        // private class InheritTypeBlock : QueryListBlock
        // {
        //     public InheritTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
        //         : base(source, id, value, attr)
        //     {
        //     }
        //
        //     public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        //     {
        //         var c = flags.HasFlag(SearchPropositionFlags.NoCategory) ? null : this.category;
        //
        //         foreach (var type in ReflectionUtility.GetAllImplementations<UnityEngine.Object>())
        //         {
        //             var simplifiedName = $"{type.FullName}, {type.Assembly.GetName().Name}";
        //
        //             yield return new SearchProposition(c, simplifiedName, simplifiedName, type: this.GetType(), data: simplifiedName);
        //         }
        //     }
        // }
    }
}
