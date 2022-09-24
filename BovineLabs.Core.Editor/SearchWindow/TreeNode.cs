using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnityEngine.UIExtras
{
    class TreeNode<T>
    {
        private T m_Value;
        internal List<TreeNode<T>> m_Children = new List<TreeNode<T>>();

        public T Value { get => m_Value; set { m_Value = value; } }

        public ReadOnlyCollection<TreeNode<T>> Children {
            get { return m_Children.AsReadOnly(); }
        }

        public int ChildCount => m_Children.Count;

        public TreeNode(T value)
        {
            m_Value = value;
        }

        public TreeNode<T> this[int i] {
            get { return m_Children[i]; }
        }

        public TreeNode<T> Parent { get; internal set; }

        public TreeNode<T> AddChild(T value)
        {
            var node = new TreeNode<T>(value) { Parent = this };
            m_Children.Add(node);
            return node;
        }

        public TreeNode<T>[] AddChildren(params T[] values)
        {
            return values.Select(AddChild).ToArray();
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            return m_Children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(Value);
            foreach (var child in m_Children)
            {
                child.Traverse(action);
            }
        }

        public void Traverse(Action<TreeNode<T>> action)
        {
            action(this);
            foreach (var child in m_Children)
            {
                child.Traverse(action);
            }
        }

        public IEnumerable<T> Flatten()
        {
            return new[] { Value }.Concat(m_Children.SelectMany(x => x.Flatten()));
        }
    }
}