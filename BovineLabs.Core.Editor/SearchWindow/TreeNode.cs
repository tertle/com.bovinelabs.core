// <copyright file="TreeNode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.Editor.SearchWindow
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    internal class TreeNode<T>
    {
        private readonly List<TreeNode<T>> children;

        public TreeNode(T value)
            : this(value, new List<TreeNode<T>>())
        {
        }

        public TreeNode(T value, List<TreeNode<T>> children)
        {
            this.Value = value;
            this.children = children;
        }

        public T Value { get; set; }

        public ReadOnlyCollection<TreeNode<T>> Children => this.children.AsReadOnly();

        public int ChildCount => this.children.Count;

        public TreeNode<T> Parent { get; internal set; }

        public TreeNode<T> this[int i] => this.children[i];

        public TreeNode<T> AddChild(T value)
        {
            var node = new TreeNode<T>(value) { Parent = this };
            this.children.Add(node);
            return node;
        }

        public TreeNode<T>[] AddChildren(params T[] values)
        {
            return values.Select(this.AddChild).ToArray();
        }

        public bool RemoveChild(TreeNode<T> node)
        {
            return this.children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(this.Value);
            foreach (var child in this.children)
            {
                child.Traverse(action);
            }
        }

        public void Traverse(Action<TreeNode<T>> action)
        {
            action(this);
            foreach (var child in this.children)
            {
                child.Traverse(action);
            }
        }

        public IEnumerable<T> Flatten()
        {
            return new[] { this.Value }.Concat(this.children.SelectMany(x => x.Flatten()));
        }
    }
}
