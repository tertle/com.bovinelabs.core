// <copyright file="ReorderableListView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ReorderableListView<T> : VisualElement
    {
        // callback when the list is reordered
        public delegate void ListReorderedDelegate(ReorderableList reorderableList, List<T> reorderedList);

        // callback when the list is reordered
        public delegate void ListSelectedDelegate(ReorderableList reorderableList, int index);

        // callback to override how an item is removed from the list
        public delegate void RemoveItemDelegate(ReorderableList reorderableList, List<T> list, int itemIndex);

        private readonly bool allowReorder;
        private readonly string headerLabel;
        private GUIStyle labelStyle;

        private ReorderableList reorderableList;
        private int selectedIndex = -1;

        public ReorderableListView(List<T> dataList, string header, bool allowReorder = true)
        {
            this.DataList = dataList;
            this.headerLabel = header;
            this.allowReorder = allowReorder;

            var container = new IMGUIContainer(this.OnGUIHandler) { name = "ListContainer" };
            this.Add(container);
        }

        public ReorderableList.AddDropdownCallbackDelegate AddDropdown { get; set; }

        public RemoveItemDelegate ItemRemoved { get; set; }

        public ListSelectedDelegate Select { get; set; }

        public ListReorderedDelegate Reordered { get; set; }

        // this is how we get the string to display for each item
        public Func<T, string> GetDisplayName { get; set; } = data => data.ToString();

        public ReorderableList.HeaderCallbackDelegate DrawHeader { get; set; }

        public ReorderableList.ElementCallbackDelegate DrawElement { get; set; }

        public ReorderableList.ElementHeightCallbackDelegate GetHeight { get; set; }

        public int Index
        {
            get => this.reorderableList.index;
            set => this.reorderableList.index = value;
        }

        // the List we are editing
        protected List<T> DataList { get; private set; }

        internal void RecreateList(List<T> dataList)
        {
            this.DataList = dataList;

            // Create reorderable list from data list
            this.reorderableList = new ReorderableList(
                dataList,
                typeof(T), // the type of the elements in dataList
                this.allowReorder, // draggable (to reorder)
                true, // displayHeader
                true, // displayAddButton
                true); // displayRemoveButton
        }

        private void OnGUIHandler()
        {
            try
            {
                if (this.reorderableList == null)
                {
                    this.RecreateList(this.DataList);
                    this.AddCallbacks();
                }

                using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
                {
                    this.reorderableList.index = this.selectedIndex;
                    this.reorderableList.DoLayoutList();

                    if (changeCheckScope.changed)
                    {
                        // Do things when changed
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void AddCallbacks()
        {
            this.reorderableList.drawHeaderCallback = this.DrawHeader ?? (rect =>
            {
                var labelRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
                EditorGUI.LabelField(labelRect, this.headerLabel);
            });

            // Draw Element
            this.reorderableList.drawElementCallback = this.DrawElement ?? ((rect, index, _, _) =>
            {
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight),
                    this.GetDisplayName(this.DataList[index]),
                    EditorStyles.label);
            });

            // Element height
            this.reorderableList.elementHeightCallback = this.GetHeight ?? (_ => this.reorderableList.elementHeight);

            // Add callback delegates
            this.reorderableList.onSelectCallback += this.SelectEntry; // should we propagate this up if user wants to do something with selection?
            this.reorderableList.onReorderCallback += this.ReorderEntries;
            this.reorderableList.onAddDropdownCallback += this.AddDropdown;
            this.reorderableList.onRemoveCallback += this.OnRemove;
        }

        private void SelectEntry(ReorderableList list)
        {
            this.selectedIndex = list.index;
            this.Select?.Invoke(list, list.index);
        }

        private void ReorderEntries(ReorderableList list)
        {
            this.Reordered?.Invoke(list, this.DataList);
        }

        private void OnRemove(ReorderableList list)
        {
            var indexToRemove = list.index;
            if (indexToRemove < 0)
            {
                indexToRemove = this.DataList.Count - 1;
            }

            if (indexToRemove >= 0)
            {
                this.ItemRemoved(list, this.DataList, indexToRemove);
            }
        }
    }
}
