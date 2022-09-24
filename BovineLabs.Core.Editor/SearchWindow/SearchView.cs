using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIExtras
{
    public class SearchView : VisualElement
    {
        public event Action<Item> OnSelection;

        SearchField m_SearchField;
        Button m_ReturnButton;
        VisualElement m_ReturnIcon;
        ListView m_List;

        public List<Item> Items
        {
            get => m_Items;
            set
            {
                m_Items = value;
                Reset();
            }
        }

        public struct Item
        {
            public string Path;
            public Texture2D Icon;
            public object Data;

            /*public Func<Status, Item> StatusCallback;
            public enum Status
            {
                Normal,
                Disabled
            }*/

            public string Name => System.IO.Path.GetFileName(Path);
        }

        List<Item> m_Items;

        string m_Title;

        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                RefreshTitle();
            }
        }

        TreeNode<Item> m_RootNode;
        TreeNode<Item> m_CurrentNode;
        TreeNode<Item> m_SearchNode;

        public SelectionType SelectionType
        {
            get => m_List.selectionType;
            set { m_List.selectionType = value; }
        }

        public SearchView()
        {
            AddToClassList("SearchView");
            if (EditorGUIUtility.isProSkin)
            {
                AddToClassList("UnityThemeDark");
            }
            else
            {
                AddToClassList("UnityThemeLight");
            }

            styleSheets.Add(Resources.Load<StyleSheet>("SearchViewStyle"));
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("SearchView");
            visualTree.CloneTree(this);

            m_SearchField = this.Q<SearchField>();
            m_ReturnButton = this.Q<Button>("ReturnButton");
            m_ReturnButton.clicked += OnNavigationReturn;
            m_ReturnIcon = this.Q("ReturnIcon");
            m_List = this.Q<ListView>("SearchResults");
            m_List.selectionType = SelectionType.Single;
            m_List.makeItem = () => { return new SearchViewItem(); };
            m_List.bindItem = (VisualElement element, int index) =>
            {
                SearchViewItem searchItem = element as SearchViewItem;
                searchItem.Item = m_CurrentNode[index];
            };

            m_List.onSelectionChange += OnListSelectionChange;
            m_List.onItemsChosen += OnItemsChosen;


            Title = "Root";

            m_SearchField.RegisterValueChangedCallback(OnSearchQueryChanged);
        }

        void OnSearchQueryChanged(ChangeEvent<string> changeEvent)
        {
            if (m_SearchNode != null && m_CurrentNode == m_SearchNode)
            {
                m_CurrentNode = m_SearchNode.Parent;
                m_SearchNode = null;
                if (changeEvent.newValue.Length == 0)
                {
                    SetCurrentSelectionNode(m_CurrentNode);
                    return;
                }
            }

            if (changeEvent.newValue.Length == 0)
            {
                return;
            }

            List<TreeNode<Item>> searchResults = new List<TreeNode<Item>>();
            m_RootNode.Traverse(delegate(TreeNode<Item> itemNode)
            {
                if (itemNode.Value.Name.IndexOf(changeEvent.newValue, StringComparison.CurrentCultureIgnoreCase) != -1)
                {
                    searchResults.Add(itemNode);
                }
            });
            m_SearchNode = new TreeNode<Item>(new Item { Path = "Search" });
            m_SearchNode.m_Children = searchResults;
            m_SearchNode.Parent = m_CurrentNode;
            SetCurrentSelectionNode(m_SearchNode);

        }

        void OnListSelectionChange(IEnumerable<object> selection)
        {
            if (SelectionType == SelectionType.Single)
            {
                OnItemsChosen(selection);
            }
            else
            {
                // TBD.
            }
        }

        void OnItemsChosen(IEnumerable<object> selection)
        {
            TreeNode<Item> node = selection.First() as TreeNode<Item>;
            if (node.ChildCount == 0)
            {
                OnSelection?.Invoke(node.Value);
            }
            else
            {
                SetCurrentSelectionNode(node);
            }
        }

        void RefreshTitle()
        {
            if (m_RootNode != null)
            {
                m_RootNode.Value = new Item { Path = m_Title, Data = null, Icon = null };
            }

            if (m_CurrentNode == null)
            {
                m_ReturnButton.text = m_Title;
                return;
            }

            m_ReturnButton.text = m_CurrentNode.Value.Name;
        }

        public void Reset()
        {
            m_RootNode = new TreeNode<Item>(new Item { Path = m_Title, Data = null, Icon = null });
            for (int i = 0; i < m_Items.Count; ++i)
            {
                Add(m_Items[i]);
            }

            SetCurrentSelectionNode(m_RootNode);
        }

        void SetCurrentSelectionNode(TreeNode<Item> node)
        {
            m_CurrentNode = node;
            m_List.itemsSource = m_CurrentNode.Children;
            m_ReturnButton.text = m_CurrentNode.Value.Name;
            m_List.RefreshItems();

            if (node.Parent == null)
            {
                m_ReturnButton.SetEnabled(false);
                m_ReturnIcon.style.visibility = Visibility.Hidden;
            }
            else
            {
                m_ReturnButton.SetEnabled(true);
                m_ReturnIcon.style.visibility = Visibility.Visible;
            }
        }

        void OnNavigationReturn()
        {
            if (m_CurrentNode != null && m_CurrentNode.Parent != null)
            {
                SetCurrentSelectionNode(m_CurrentNode.Parent);
            }
        }

        void Add(Item item)
        {
            if (item.Path.Length == 0)
            {
                return;
            }

            string[] pathParts = item.Path.Split('/');
            TreeNode<Item> parent = m_RootNode;
            string currentPath = string.Empty;
            for (int i = 0; i < pathParts.Length; ++i)
            {
                if (currentPath.Length == 0)
                {
                    currentPath += pathParts[i];
                }
                else
                {
                    currentPath += "/" + pathParts[i];
                }

                TreeNode<Item> node = FindNodeByPath(parent, currentPath);
                if (node == null)
                {
                    node = parent.AddChild(new Item { Path = currentPath, Data = null, Icon = null });
                }

                if (i == (pathParts.Length - 1))
                {
                    node.Value = item;
                }
                else
                {
                    parent = node;
                }
            }
        }

        TreeNode<Item> FindNodeByPath(TreeNode<Item> parent, string path)
        {
            if (parent == null || path.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < parent.ChildCount; ++i)
            {
                if (parent[i].Value.Path.Equals(path))
                {
                    return parent[i];
                }
            }

            return null;
        }
    }
}
