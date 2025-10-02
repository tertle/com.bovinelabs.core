// <copyright file="IDropDown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SubScenes
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public interface IDropDown
    {
        void AddItem(string text, bool isChecked, Action<object> action, object userData);

        void AddSeparator(string path);

        void Finish();
    }

    public class DropdownMenuWrapper : IDropDown
    {
        private readonly DropdownMenu menu;

        public DropdownMenuWrapper(DropdownMenu menu)
        {
            this.menu = menu;
        }

        public void AddItem(string text, bool isChecked, Action<object> action, object userData)
        {
            var status = isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            this.menu.AppendAction(text, _ => action(userData), status);
        }

        public void AddSeparator(string path)
        {
            this.menu.AppendSeparator(path);
        }

        public void Finish()
        {
        }
    }

    public class GenericMenuWrapper : IDropDown
    {
        private readonly GenericMenu menu = new();
        private readonly Rect worldBound;

        public GenericMenuWrapper(Rect worldBound)
        {
            this.worldBound = worldBound;
        }

        public void AddItem(string text, bool isChecked, Action<object> action, object userData)
        {
            this.menu.AddItem(EditorGUIUtility.TrTextContent(text), isChecked, data => action(data), userData);
        }

        public void AddSeparator(string path)
        {
            this.menu.AddSeparator(path);
        }

        public void Finish()
        {
            this.menu.DropDown(this.worldBound);
        }
    }
}
