// <copyright file="SearchBridge.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Internal
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Search;
    using UnityEngine;

    public static class SearchBridge
    {
        public static IQueryEngineFilter SetFilter<TFilter, TData>(QueryEngine<TData> queryEngine, string? token, Func<TData, TFilter> getDataFunc, string[]? supportedOperatorType = null)
        {
            return Unity.Editor.Bridge.SearchBridge.SetFilter(queryEngine, token, getDataFunc, supportedOperatorType);
        }

        public static IQueryEngineFilter AddOrUpdateProposition(this IQueryEngineFilter filter, string label, string? category = null, string? replacement = null, string? help = null, string? data = null,
            int priority = 0, Texture2D? icon = null, Type? type = null, Color color = default, TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete)
        {
            return Unity.Editor.Bridge.SearchBridge.AddOrUpdateProposition(
                filter, label, category, replacement, help, data, priority, icon, type, color, moveCursor);
        }

        public static void AddFilter<TFilter, TData>(
            QueryEngine<TData> queryEngine, string token, Func<TData, QueryFilterOperator, TFilter, bool> filterResolver, string[]? supportedOperatorType = null)
        {
            Unity.Editor.Bridge.SearchBridge.AddFilter(queryEngine, token, filterResolver, supportedOperatorType);
        }

        public static IEnumerable<SearchProposition> GetPropositions<TData>(QueryEngine<TData> qe)
        {
            return Unity.Editor.Bridge.SearchBridge.GetPropositions(qe);
        }

        public static IEnumerable<SearchProposition> GetPropositionsFromListBlockType(Type t)
        {
            return Unity.Editor.Bridge.SearchBridge.GetPropositionsFromListBlockType(t);
        }
    }
}
