using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEngine.UIExtras
{
    public class SearchWindow : EditorWindow
    {
        SearchView m_SearchView;
        public List<SearchView.Item> Items {
            get => m_SearchView.Items;
            set {
                m_SearchView.Items = value;
            }
        }
        public event Action<SearchView.Item> OnSelection;
        public string Title {
            get => m_SearchView.Title;
            set {
                m_SearchView.Title = value;
            }
        }

        private void OnEnable()
        {
            m_SearchView = new SearchView();
            rootVisualElement.Add(m_SearchView);
            rootVisualElement.style.color = Color.white;
            m_SearchView.OnSelection += (e) =>
            {
                OnSelection?.Invoke(e);
                Close();
            };
        }

        public static SearchWindow Create()
        {
            SearchWindow window = EditorWindow.CreateInstance<SearchWindow>();
            return window;
        }


        private void OnFocus()
        {
            m_SearchView.Q<SearchField>().Q("unity-text-input").Focus();
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}
