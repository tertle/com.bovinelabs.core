// <copyright file="ToolbarSystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Debug.Toolbar
{
    using System;
    using Unity.Entities;
    using UnityEngine.UIElements;

    /// <summary>
    /// Base class for a toolbar group. Implement this to add your own element to the toolbar.
    /// Your system should update in one of the following groups depending what it does.
    /// </summary>
    public abstract partial class ToolbarSystemBase : SystemBase
    {
        protected const float DefaultUpdateRate = 1 / 4f;

        private ToolbarTab.Group tabGroup;

        /// <summary> Gets the key for loading the panel asset. </summary>
        protected abstract VisualTreeAsset Asset { get; }

        /// <summary> Gets the name of the tab the toolbar should go under. </summary>
        protected virtual string Tab => this.World.Name.EndsWith("World")
            ? this.World.Name[..this.World.Name.LastIndexOf("World", StringComparison.Ordinal)]
            : this.World.Name;

        /// <summary> Gets the name of the toolbar. </summary>
        protected abstract string Name { get; }

        /// <summary> Gets the toolbar panel. Invalid until <see cref="OnLoad" /> has been called. </summary>
        protected TemplateContainer Panel { get; private set; }

        /// <inheritdoc />
        protected sealed override void OnCreate()
        {
            this.OnCreateSystem();

            this.Panel = this.Asset.CloneTree();
            this.OnLoad(this.Panel);

            this.tabGroup = new ToolbarTab.Group(this.Name, this.Panel);
        }

        /// <inheritdoc />
        protected sealed override void OnDestroy()
        {
            this.OnDestroySystem();
        }

        /// <inheritdoc />
        protected sealed override void OnStartRunning()
        {
            ToolbarManager.AddGroup(this.Tab, this.tabGroup);
            this.OnStartRunningSystem();
        }

        /// <inheritdoc />
        protected sealed override void OnStopRunning()
        {
            ToolbarManager.RemoveGroup(this.Tab, this.tabGroup);
            this.OnStopRunningSystem();
        }

        /// <inheritdoc />
        protected sealed override void OnUpdate()
        {
            this.OnUpdateAlways();

            if (ToolbarManager.IsTabVisible(this.Tab))
            {
                this.OnUpdateVisible();
            }
        }

        /// <summary> Implement to setup UI after the element has finished async loading. </summary>
        /// <param name="element"> The toolbar panel. </param>
        protected abstract void OnLoad(VisualElement element);

        /// <summary> Replacement for <see cref="OnCreate" />. </summary>
        protected virtual void OnCreateSystem()
        {
        }

        /// <summary> Replacement for <see cref="OnDestroy" />. </summary>
        protected virtual void OnDestroySystem()
        {
        }

        /// <summary> Replacement for <see cref="OnStartRunning" />. </summary>
        protected virtual void OnStartRunningSystem()
        {
        }

        /// <summary> Replacement for <see cref="OnStopRunning" />. </summary>
        protected virtual void OnStopRunningSystem()
        {
        }

        /// <summary> Override to get updates every frame. </summary>
        protected virtual void OnUpdateAlways()
        {
        }

        /// <summary> Override to get updates only when the toolbar is visible. </summary>
        protected virtual void OnUpdateVisible()
        {
        }
    }
}
#endif
