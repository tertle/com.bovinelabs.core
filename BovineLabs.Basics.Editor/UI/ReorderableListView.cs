// <copyright file="ReorderableListView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Editor.UI
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
        private ReorderableList reorderableList;

        private IMGUIContainer m_Container;
        private GUIStyle m_LabelStyle;
        private int m_SelectedIndex = -1;
        private string m_HeaderLabel;

        public ReorderableList.AddDropdownCallbackDelegate AddDropdown { get; set; }

        // callback to override how an item is removed from the list
        public delegate void RemoveItemDelegate(List<T> list, int itemIndex);

        public RemoveItemDelegate ItemRemoved { get; set; }

        // callback when the list is reordered
        public delegate void ListReorderedDelegate(List<T> reorderedList);

        public ListReorderedDelegate Reordered { get; set; }

        public ReorderableListView(List<T> dataList, string header, bool allowReorder = true)
        {
            this.DataList = dataList;
            this.m_HeaderLabel = header;
            this.allowReorder = allowReorder;

            // should we set up a new style sheet?  allow user overrides?
            //this.styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));

            this.m_Container = new IMGUIContainer(this.OnGUIHandler) { name = "ListContainer" };
            this.Add(this.m_Container);
        }

        // the List we are editing
        protected List<T> DataList { get; private set; }

        // this is how we get the string to display for each item
        public Func<T, string> GetDisplayName { get; set; } = (data => data.ToString());

        public ReorderableList.HeaderCallbackDelegate DrawHeader { get; set; }
        public ReorderableList.ElementCallbackDelegate DrawElement { get; set; }
        public ReorderableList.ElementHeightCallbackDelegate GetHeight { get; set; }


        internal void RecreateList(List<T> dataList)
        {
            this.DataList = dataList;

            // Create reorderable list from data list
            this.reorderableList = new ReorderableList(
                dataList,
                typeof(T),          // the type of the elements in dataList
                this.allowReorder,     // draggable (to reorder)
                true,               // displayHeader
                true,               // displayAddButton
                true);              // displayRemoveButton
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
                    this.reorderableList.index = this.m_SelectedIndex;
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
                EditorGUI.LabelField(labelRect, this.m_HeaderLabel);
            });

            // Draw Element
            this.reorderableList.drawElementCallback = this.DrawElement ?? ((rect, index, isActive, isFocused) =>
            {
                EditorGUI.LabelField(
                    new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight),
                    this.GetDisplayName(this.DataList[index]),
                    EditorStyles.label);
            });

            // Element height
            this.reorderableList.elementHeightCallback = this.GetHeight ?? (indexer => this.reorderableList.elementHeight);

            // Add callback delegates
            this.reorderableList.onSelectCallback += this.SelectEntry;              // should we propagate this up if user wants to do something with selection?
            this.reorderableList.onReorderCallback += this.ReorderEntries;
            this.reorderableList.onAddDropdownCallback += this.AddDropdown;
            this.reorderableList.onRemoveCallback += this.OnRemove;
        }

        private void SelectEntry(ReorderableList list)
        {
            this.m_SelectedIndex = list.index;
        }
        //
        // protected void OnAddMenuClicked(T optionText)
        // {
        //     this.ItemAdded(this.DataList, optionText);
        // }

        private void ReorderEntries(ReorderableList list)
        {
            this.Reordered?.Invoke(this.DataList);
        }

        // protected abstract void OnAddDropdownMenu(Rect buttonRect, ReorderableList list);

        private void OnRemove(ReorderableList list)
        {
            int indexToRemove = list.index;
            if (indexToRemove < 0)
            {
                indexToRemove = this.DataList.Count - 1;
            }

            if (indexToRemove >= 0)
            {
                this.ItemRemoved(this.DataList, indexToRemove);
            }
        }
    }
}

