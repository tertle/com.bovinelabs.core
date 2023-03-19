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
        private readonly bool allowReorder;
        private readonly string headerLabel;

        private readonly ReorderableList.AddDropdownCallbackDelegate addDropdown;
        private readonly RemoveItemDelegate itemRemoved;
        private readonly ReorderableList.ElementCallbackDelegate drawElement;

        private ReorderableList.ElementHeightCallbackDelegate getHeight;
        private ReorderableList.HeaderCallbackDelegate drawHeader;

        private ReorderableList reorderableList;
        private int selectedIndex = -1;

        // callback when the list is reordered
        public delegate void ListReorderedDelegate(ReorderableList reorderableList, List<T> reorderedList);

        // callback when the list is reordered
        public delegate void ListSelectedDelegate(ReorderableList reorderableList, int index);

        // callback to override how an item is removed from the list
        public delegate void RemoveItemDelegate(ReorderableList reorderableList, List<T> list, int itemIndex);

        public ReorderableListView(
            List<T> dataList,
            string header,
            ReorderableList.AddDropdownCallbackDelegate addDropdown,
            RemoveItemDelegate itemRemoved,
            ReorderableList.ElementCallbackDelegate drawElement,
            bool allowReorder = true)
        {
            this.DataList = dataList;
            this.headerLabel = header;

            this.addDropdown = addDropdown;
            this.itemRemoved = itemRemoved;
            this.drawElement = drawElement;

            this.allowReorder = allowReorder;

            this.drawHeader = this.DefaultDrawHeader;
            this.getHeight = this.DefaultGetHeight;

            this.reorderableList = this.RecreateList();
            this.AddCallbacks();

            var container = new IMGUIContainer(this.OnGUIHandler) { name = "ListContainer" };
            this.Add(container);
        }

        public event ListSelectedDelegate? Select;

        public event ListReorderedDelegate? Reordered;

        public ReorderableList.HeaderCallbackDelegate DrawHeader
        {
            get => this.drawHeader;
            set
            {
                this.drawHeader = value;
                this.reorderableList.drawHeaderCallback = this.DrawHeader;
            }
        }

        public ReorderableList.ElementHeightCallbackDelegate GetHeight
        {
            get => this.getHeight;
            set
            {
                this.getHeight = value;
                this.reorderableList.elementHeightCallback = this.GetHeight;
            }
        }

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
            this.reorderableList = this.RecreateList();
            this.AddCallbacks();
        }

        private ReorderableList RecreateList()
        {
            // Create reorderable list from data list
            var list = new ReorderableList(
                this.DataList,
                typeof(T), // the type of the elements in dataList
                this.allowReorder, // draggable (to reorder)
                true, // displayHeader
                true, // displayAddButton
                true); // displayRemoveButton

            return list;
        }

        private void OnGUIHandler()
        {
            try
            {
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();
                this.reorderableList.index = this.selectedIndex;
                this.reorderableList.DoLayoutList();

                if (changeCheckScope.changed)
                {
                    // Do things when changed
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void AddCallbacks()
        {
            this.reorderableList.drawHeaderCallback = this.DrawHeader;

            // Draw Element
            this.reorderableList.drawElementCallback = this.drawElement;

            // Element height
            this.reorderableList.elementHeightCallback = this.GetHeight;
            this.reorderableList.onAddDropdownCallback = this.addDropdown;
            this.reorderableList.onRemoveCallback = this.OnRemove;

            // Add callback delegates
            this.reorderableList.onSelectCallback = this.SelectEntry;
            this.reorderableList.onReorderCallback = this.ReorderEntries;
        }

        private void DefaultDrawHeader(Rect rect)
        {
            var labelRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
            EditorGUI.LabelField(labelRect, this.headerLabel);
        }

        private float DefaultGetHeight(int index) => this.reorderableList.elementHeight;

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
                this.itemRemoved(list, this.DataList, indexToRemove);
            }
        }
    }
}
