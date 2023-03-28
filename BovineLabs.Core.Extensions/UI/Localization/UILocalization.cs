// <copyright file="UILocalization.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI && UNITY_LOCALIZATION && BL_LOCALIZATION
namespace BovineLabs.Core.UI.Localization
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Tables;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.UIElements;

    public class UILocalization : IDisposable
    {
        private readonly VisualElement parent;
        private readonly LocalizedStringTable stringTable;
        private readonly Dictionary<TextElement, string> textElements = new();
        private EntityQuery debugQuery;

        public UILocalization(ref SystemState state, Guid stringLocalization, VisualElement parent)
            : this(ref state, new LocalizedStringTable(stringLocalization), parent)
        {
        }

        public UILocalization(ref SystemState state, LocalizedStringTable stringTable, VisualElement parent)
        {
            this.debugQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<BLDebug>().Build(ref state);
            this.parent = parent;

            this.GetAllValidTextElementsRecursively(parent);

            this.stringTable = stringTable;
            this.stringTable.TableChanged += this.OnStringTableChanged;
            this.OnStringTableChanged();
        }

        public void Dispose()
        {
            this.stringTable.TableChanged -= this.OnStringTableChanged;
        }

        private void GetAllValidTextElementsRecursively(VisualElement element)
        {
            var elementHierarchy = element.hierarchy;
            var childCount = elementHierarchy.childCount;
            for (var i = 0; i < childCount; i++)
            {
                if (elementHierarchy[i] is TextElement textElement)
                {
                    var key = textElement.text;
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    if (key[0] != '#')
                    {
                        continue;
                    }

                    key = key.Substring(1, key.Length - 1);
                    this.textElements.Add(textElement, key);
                }
            }

            for (var i = 0; i < childCount; i++)
            {
                this.GetAllValidTextElementsRecursively(elementHierarchy[i]);
            }
        }

        private void OnStringTableChanged(StringTable value)
        {
            this.OnStringTableChanged();
        }

        private void OnStringTableChanged()
        {
            try
            {
                var op = this.stringTable.GetTableAsync();
                op.Completed -= this.OnTableLoaded;
                op.Completed += this.OnTableLoaded;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnTableLoaded(AsyncOperationHandle<StringTable> op)
        {
            var currentTable = op.Result;

            foreach (var group in this.textElements)
            {
                var entry = currentTable[group.Value];
                if (entry == null)
                {
                    this.debugQuery.GetSingleton<BLDebug>().Warning($"No translation in {currentTable.LocaleIdentifier} for key: {group.Value}");
                    group.Key.text = $"#{group.Value}";
                }
                else
                {
                    group.Key.text = entry.LocalizedValue;
                }
            }

            this.parent.MarkDirtyRepaint();
        }
    }
}
#endif
