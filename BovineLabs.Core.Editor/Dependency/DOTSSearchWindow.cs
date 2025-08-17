// <copyright file="DOTSSearchWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Dependency
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Resources = Unity.Entities.Editor.Resources;

    internal abstract class DOTSSearchWindow : DOTSEditorWindow
    {
        private static readonly List<SearchView.Item> Items = new();

        private VisualElement root = null!;

        protected DOTSSearchWindow()
            : base(Analytics.Window.Unknown)
        {
        }

        protected World? World { get; private set; }

        protected VisualElement View { get; private set; } = null!;

        protected ToolbarButton Button { get; private set; } = null!;

        protected abstract string WindowName { get; }

        protected abstract string DefaultButtonText { get; }

        protected abstract void PopulateItems(List<SearchView.Item> items);

        protected abstract void SearchWindowOnOnSelection(SearchView.Item item);

        protected abstract void Rebuild();

        protected abstract VisualElement CreateView();

        protected void ClearItems()
        {
            Items.Clear();
        }

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            Resources.AddCommonVariables(this.rootVisualElement);

            this.titleContent = EditorGUIUtility.TrTextContent(this.WindowName, EditorIcons.System);
            this.minSize = Constants.MinWindowSize;

            this.root = new VisualElement();
            this.root.AddToClassList(UssClasses.SystemScheduleWindow.WindowRoot);
            this.rootVisualElement.Add(this.root);

            Resources.Templates.SystemSchedule.AddStyles(this.root);
            Resources.Templates.DotsEditorCommon.AddStyles(this.root);

            this.CreateToolBar(this.root);

            this.View = this.CreateView();
            this.root.Add(this.View);
        }

        /// <inheritdoc/>
        protected override void OnWorldSelected(World newWorld)
        {
            this.World = newWorld;

            if (this.World != null)
            {
                this.Rebuild();
            }
        }

        private void CreateToolBar(VisualElement rootElement)
        {
            Resources.Templates.SystemScheduleToolbar.Clone(rootElement);
            var leftSide = rootElement.Q(className: UssClasses.SystemScheduleWindow.Toolbar.LeftSide);

            var worldSelector = this.CreateWorldSelector();
            leftSide.Add(worldSelector);
            worldSelector.SetVisibility(true);

            var rightSide = rootElement.Q(className: UssClasses.SystemScheduleWindow.Toolbar.RightSide);

            this.Button = new ToolbarButton { text = this.DefaultButtonText };
            this.Button.clicked += this.ButtonOnClicked;

            rightSide.Add(this.Button);
        }

        private void ButtonOnClicked()
        {
            if (this.World == null)
            {
                return;
            }

            if (Items.Count == 0)
            {
                this.PopulateItems(Items);
            }

            var searchWindow = SearchWindow.Create();

            // searchWindow.Title = "Components";
            searchWindow.Items = Items;
            searchWindow.OnSelection += this.SearchWindowOnOnSelection;
            var rect = focusedWindow.position;
            var worldBounds = this.Button.worldBound;

            var size = new Rect(rect.x + worldBounds.x, rect.y + worldBounds.y + worldBounds.height, 400, 400);
            searchWindow.position = size;
            searchWindow.ShowPopup();
        }
    }
}
