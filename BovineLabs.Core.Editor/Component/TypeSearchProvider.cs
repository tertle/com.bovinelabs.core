// <copyright file="TypeSearchProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;
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

        [SearchActionsProvider]
        private static IEnumerable<SearchAction> CreateActions()
        {
            yield return new SearchAction(TypeAsset.SearchProviderType, "inspect", null, "Inspect ECS component")
            {
                enabled = items => items.Count == 1 && TryGetTypeInfo(items.First(), out _),
                execute = items => InspectComponent(items[0]),
            };
            yield return new SearchAction(TypeAsset.SearchProviderType, "copy", null, "Copy full type name")
            {
                execute = items => EditorGUIUtility.systemCopyBuffer = Type.GetType((string)items[0].data).FullName,
            };
        }

        private static void InspectComponent(SearchItem item)
        {
            if (!TryGetTypeInfo(item, out var info))
            {
                return;
            }

            using var context = SearchService.CreateContext("component", $"index={info.TypeIndex.Value}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);
            foreach (var result in results)
            {
                result.provider.trackSelection(result, context);
            }
        }

        private static bool TryGetTypeInfo(SearchItem item, out TypeManager.TypeInfo info)
        {
            TypeManager.Initialize();
            var type = Type.GetType((string)item.data);
            foreach (var candidate in TypeManager.AllTypes)
            {
                if (candidate.Type == type && candidate.Category != TypeManager.TypeCategory.UnityEngineObject)
                {
                    info = candidate;
                    return true;
                }
            }

            info = default;
            return false;
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
                    context.AddSearchQueryErrors(query.errors.Select(error => new SearchQueryError(error, context, provider)));
                    yield break;
                }
            }

            var descriptors = GetTypeDescriptors(searchQuery);
            var score = 0;
            foreach (var data in query?.Apply(descriptors) ?? descriptors)
            {
                var description = data.SimplifiedQualifiedName;
                if (data.TypeIndexValue != 0)
                {
                    description += $" | TypeIndex: {data.TypeIndexValue} | StableTypeHash: {data.StableTypeHash} (0x{data.StableTypeHash:X16})";
                }

                yield return provider.CreateItem(context, data.FullName, score++, data.Name, description, null, data.FullName);
            }
        }

        private static IEnumerable<TypeDescriptor> GetTypeDescriptors(string searchQuery)
        {
            if (UsesEcsFilters(searchQuery))
            {
                TypeManager.Initialize();
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

            return Regex.IsMatch(searchQuery, @"\b(component|componentdata|enableable|zerosized|typeindex|stabletypehash)\s*[:=!]",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            yield return new SearchProposition(
                category: null, label: "Is Unmanaged", replacement: "unmanaged=true", help: "Limit search to unmanaged types");
            yield return new SearchProposition(
                category: null, label: "Is Unity Object", replacement: "unityobject=true", help: "Limit search to Unity Objects");
            yield return new SearchProposition(
                category: null, label: "Is ECS Component", replacement: "component=true", help: "Limit search to component and buffer types");
            yield return new SearchProposition(
                category: null, label: "Is Component Data", replacement: "componentdata=true", help: "Limit search to component data types");
            yield return new SearchProposition(
                category: null, label: "Is Enableable", replacement: "enableable=true", help: "Limit search to enableable component types");
            yield return new SearchProposition(
                category: null, label: "Is Zero Sized", replacement: "zerosized=true", help: "Limit search to zero-sized component types");
            yield return new SearchProposition(
                category: null, label: "Is Editor Assembly", replacement: "editor=true", help: "Limit search to types in editor assemblies");
            yield return new SearchProposition(
                category: null, label: "Type Index", replacement: "typeindex=123", help: "Find an ECS type by its full or masked TypeIndex in this session");
            yield return new SearchProposition(
                category: null, label: "Stable Type Hash", replacement: "stabletypehash=0x1234", help: "Find an ECS type by its exact decimal or hex hash");
        }

        private static QueryEngine<TypeDescriptor> SetupQueryEngine()
        {
            var query = new QueryEngine<TypeDescriptor>();
            query.SetSearchDataCallback(GetWords);

            query.AddFilter("unmanaged", data => data.IsUnmanaged);
            query.AddFilter("unityobject", data => data.IsUnityObject);
            query.AddFilter("component", data => data.IsComponent);
            query.AddFilter("componentdata", data => data.IsComponentData);
            query.AddFilter("enableable", data => data.IsEnableable);
            query.AddFilter("zerosized", data => data.IsZeroSized);
            query.AddFilter("editor", data => data.IsEditorAssembly);
            query.AddFilter<int>("typeindex", (data, _, value) => data.TypeIndexValue == value || data.TypeIndexWithoutFlags == value, new[] { "=", ":" });
            query.AddFilter<ulong>("stabletypehash", (data, _, value) => data.StableTypeHash == value, new[] { "=", ":" });
            query.AddTypeParser<int>(ParseTypeIndex);
            query.AddTypeParser<ulong>(ParseStableTypeHash);

            query.AddFilter<string>("inherit", OnInheritFilter, /*Transformer,*/ new[] { "=", ":" });

            return query;
        }

        private static ParseResult<int> ParseTypeIndex(string text)
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var bits)
                    ? new ParseResult<int>(true, unchecked((int)bits))
                    : ParseResult<int>.none;
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? new ParseResult<int>(true, value)
                : ParseResult<int>.none;
        }

        private static ParseResult<ulong> ParseStableTypeHash(string text)
        {
            var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            return ulong.TryParse(hex ? text.Substring(2) : text, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                CultureInfo.InvariantCulture, out var value)
                ? new ParseResult<ulong>(true, value)
                : ParseResult<ulong>.none;
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
            private readonly ulong stableTypeHash;

            public TypeDescriptor(Type type)
            {
                this.Type = type;
                this.typeIndex = TypeIndex.Null;
                this.category = TypeManager.TypeCategory.UnityEngineObject;
                this.isZeroSized = false;
                this.stableTypeHash = 0;
            }

            public TypeDescriptor(TypeManager.TypeInfo typeInfo)
            {
                this.Type = typeInfo.Type;
                this.typeIndex = typeInfo.TypeIndex;
                this.category = typeInfo.Category;
                this.isZeroSized = typeInfo.IsZeroSized;
                this.stableTypeHash = typeInfo.StableTypeHash;
            }

            public string Name => this.Type.Name;

            public string SimplifiedQualifiedName => $"{this.Type.FullName}, {this.Type.Assembly.GetName().Name}";

            public string FullName => this.Type.AssemblyQualifiedName;

            public int TypeIndexValue => this.typeIndex.Value;

            public int TypeIndexWithoutFlags => this.typeIndex.Index;

            public ulong StableTypeHash => this.stableTypeHash;

            public bool IsUnmanaged => UnsafeUtility.IsUnmanaged(this.Type);

            public bool IsUnityObject => typeof(UnityEngine.Object).IsAssignableFrom(this.Type);

            public bool IsComponent => this.category is TypeManager.TypeCategory.ComponentData or TypeManager.TypeCategory.BufferData;

            public bool IsComponentData => this.category == TypeManager.TypeCategory.ComponentData;

            public bool IsEnableable => this.typeIndex != TypeIndex.Null && TypeManager.IsEnableable(this.typeIndex);

            public bool IsZeroSized => this.isZeroSized;

            public bool IsEditorAssembly => this.Type.Assembly.IsAssemblyEditorAssembly() || this.Type.Assembly.IsTestEditorAssembly();
        }
    }
}
