// <copyright file="EntityIdSearchProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SearchWindow
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal static class EntityIdSearchProvider
    {
        private const string ProviderId = "bovinelabs-entity-id";

        [SearchItemProvider]
        private static SearchProvider CreateProvider()
        {
            return new SearchProvider(ProviderId, "Object Entity ID")
            {
                filterId = "eid:",
                isExplicitProvider = true,
                active = true,
                showDetails = true,
                fetchItems = FetchItems,
                toObject = (item, _) => (Object)item.data,
                trackSelection = (item, _) => EditorGUIUtility.PingObject((Object)item.data),
            };
        }

        [SearchActionsProvider]
        private static IEnumerable<SearchAction> CreateActions()
        {
            yield return new SearchAction(ProviderId, "select", null, "Select object")
            {
                execute = items => Selection.activeObject = (Object)items[0].data,
            };
            yield return new SearchAction(ProviderId, "ping", null, "Ping object")
            {
                execute = items => EditorGUIUtility.PingObject((Object)items[0].data),
            };
            yield return new SearchAction(ProviderId, "open", null, "Open object")
            {
                execute = items => AssetDatabase.OpenAsset((Object)items[0].data),
            };
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var text = context.searchQuery.Trim();
            if (text.Length == 0)
            {
                yield break;
            }

            if (!TryParse(text, out var entityId))
            {
                context.AddSearchQueryError(new SearchQueryError(0, context.searchQuery.Length,
                    "Enter an EntityId as its logged pair, unsigned decimal value, or 0x hexadecimal value.", context, provider));
                yield break;
            }

            var obj = EditorUtility.EntityIdToObject(entityId);
            if (obj == null)
            {
                yield break;
            }

            var path = AssetDatabase.GetAssetPath(obj);
            var description = $"{obj.GetType().FullName} | {entityId}";
            if (!string.IsNullOrEmpty(path))
            {
                description += $" | {path}";
            }

            yield return provider.CreateItem(context, EntityId.ToULong(entityId).ToString(CultureInfo.InvariantCulture), 0,
                obj.name, description, AssetPreview.GetMiniThumbnail(obj), obj);
        }

        private static bool TryParse(string text, out EntityId entityId)
        {
            entityId = EntityId.None;
            var colon = text.IndexOf(':');
            if (colon >= 0)
            {
                // Unity's EntityId.ToString() prints the two signed 32-bit words of the packed ID.
                if (!int.TryParse(text.Substring(0, colon), NumberStyles.Integer, CultureInfo.InvariantCulture, out var low) ||
                    !int.TryParse(text.Substring(colon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var high))
                {
                    return false;
                }

                entityId = EntityId.FromULong(((ulong)unchecked((uint)high) << 32) | unchecked((uint)low));
                return true;
            }

            var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            if (!ulong.TryParse(hex ? text.Substring(2) : text, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                    CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            entityId = EntityId.FromULong(value);
            return true;
        }
    }
}
