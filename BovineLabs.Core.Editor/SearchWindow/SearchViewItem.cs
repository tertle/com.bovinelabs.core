using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIExtras
{
    class SearchViewItem : VisualElement
    {
        Label m_Label;
        VisualElement m_Icon;
        VisualElement m_NextIcon;

        string m_Name;
        public string Name => m_Name;
        public SearchViewItem()
        {
            AddToClassList("SearchItem");
            styleSheets.Add(Resources.Load<StyleSheet>("SearchItemStyle"));
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>("SearchItem");
            visualTree.CloneTree(this);

            m_Label = this.Q<Label>("Label");
            m_Icon = this.Q("Icon");
            m_NextIcon = this.Q("NextIcon");
            tabIndex = -1;
        }

        public TreeNode<SearchView.Item> Item {
            get => userData as TreeNode<SearchView.Item>;
            set {
                userData = value;
                m_Icon.style.backgroundImage = value.Value.Icon;
                m_Name = value.Value.Name;
                m_Label.text = Name;

                if (value.ChildCount == 0)
                {
                    m_NextIcon.style.visibility = Visibility.Hidden;
                }
                else
                {
                    m_NextIcon.style.visibility = Visibility.Visible;
                }
            }
        }
    }
}