// <copyright file="TreeNode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace UnityEngine.UIExtras
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    internal class TreeNode<T>
    {
        internal List<TreeNode<T>> m_Children = new();

        public TreeNode(T value)
        {
            this.Value = value;
        }

        public T Value { get; set; }

        public ReadOnlyCollection<TreeNode<T>> Children => this.m_Children.AsReadOnly();

        public int ChildCount => this.m_Children.Count;

        public TreeNode<T> this[int i] => this.m_Children[i];

        public TreeNode<T> Parent { get; internal set; }

        public TreeNode<T> AddChild(T value)
        {
            var node = new TreeNode<T>(value) { Parent = this };
            this.m_Children.Add(node);
            return node;
        }

        public TreeNode<T>[] AddChildren(params T[] values)
        {
            return values.Select(this.AddChild).ToArray();
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            return this.m_Children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(this.Value);
            foreach (var child in this.m_Children)
            {
                child.Traverse(action);
            }
        }

        public void Traverse(Action<TreeNode<T>> action)
        {
            action(this);
            foreach (var child in this.m_Children)
            {
                child.Traverse(action);
            }
        }

        public IEnumerable<T> Flatten()
        {
            return new[] { this.Value }.Concat(this.m_Children.SelectMany(x => x.Flatten()));
        }
    }
}
