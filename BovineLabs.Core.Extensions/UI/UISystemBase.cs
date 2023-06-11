// <copyright file="UISystemBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
#if UNITY_LOCALIZATION && BL_LOCALIZATION
#define LOCALIZATION_ENABLED
#endif

namespace BovineLabs.Core.UI
{
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.States;
#if LOCALIZATION_ENABLED
    using BovineLabs.Core.UI.Localization;
#endif
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.UIElements;

    /// <summary> Base class for ui panels. </summary>
    [UpdateInGroup(typeof(UISystemGroup))]
    public abstract partial class UISystemBase : SystemBase
    {
        private const string PanelClassName = "panel";

#if LOCALIZATION_ENABLED
        private UILocalization? localization;
#endif
        private TemplateContainer panelElement = new();
        private byte stateKey;
        private bool initialized;

        protected abstract string StateName { get; }

        protected abstract TypeIndex StateInstanceComponent { get; }

        /// <summary> Gets the panel priority. </summary>
        protected virtual int Priority => PanelPriority.Default;

        /// <inheritdoc />
        protected sealed override void OnCreate()
        {
            var components = new FixedList32Bytes<ComponentType>
            {
                ComponentType.FromTypeIndex(this.StateInstanceComponent),
            };
            var query = new EntityQueryBuilder(Allocator.Temp).WithAll(ref components).Build(this);
            this.RequireForUpdate(query);
            this.RequireForUpdate<UIAssets>();

            this.stateKey = (byte)K<UIStates>.NameToKey(this.StateName);

            this.EntityManager.AddComponentData(this.SystemHandle, new StateInstance
            {
                State = TypeManager.GetTypeIndex<UIState>(),
                StateKey = this.stateKey,
                StateInstanceComponent = this.StateInstanceComponent,
            });

            this.OnCreateSystem();
        }

        /// <inheritdoc />
        protected sealed override void OnDestroy()
        {
            this.OnDestroySystem();
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            if (!this.initialized)
            {
                this.initialized = true;

                var assets = this.EntityManager.GetComponentObject<UIAssets>(SystemAPI.ManagedAPI.GetSingletonEntity<UIAssets>());
                var asset = assets.Assets[this.stateKey];

                Assert.IsNotNull(asset, $"No UI asset set for {this.StateName}");

                this.panelElement = asset.CloneTree();
                this.panelElement.name = this.StateInstanceComponent.ToString();
                this.panelElement.pickingMode = PickingMode.Ignore;
                this.panelElement.AddToClassList(PanelClassName);
                this.OnLoad(this.panelElement);

#if LOCALIZATION_ENABLED
                this.localization = new UILocalization(ref this.CheckedStateRef, assets.StringLocalization, this.panelElement);
#endif
            }

            this.OnShow(this.panelElement);
            UIDocumentManager.Instance.AddPanel(this.panelElement, this.Priority);
#if UNITY_EDITOR
            UIDocumentManager.Instance.EditorRebuild += this.OnEditorRebuild;
#endif
        }

        /// <inheritdoc />
        protected override void OnStopRunning()
        {
            this.OnHide(this.panelElement);
#if UNITY_EDITOR
            UIDocumentManager.Instance.EditorRebuild -= this.OnEditorRebuild;
#endif
            UIDocumentManager.Instance.RemovePanel(this.panelElement);

#if LOCALIZATION_ENABLED
            this.localization!.Dispose();
#endif
        }

        /// <inheritdoc />
        protected sealed override void OnUpdate()
        {
            this.OnUpdate(this.panelElement);
        }

        /// <summary> On panel update. The panel is guaranteed to have loaded at this point. </summary>
        /// <param name="panel"> The panel. </param>
        protected virtual void OnUpdate(TemplateContainer panel)
        {
        }

        /// <summary> Replacement for <see cref="OnCreate" />. </summary>
        protected virtual void OnCreateSystem()
        {
        }

        /// <summary> Replacement for <see cref="OnDestroy" />. </summary>
        protected virtual void OnDestroySystem()
        {
        }

        /// <summary> Called when the asset is loaded to allow initialization before the panel becomes visible. </summary>
        /// <param name="panel"> The panel. </param>
        protected virtual void OnLoad(VisualElement panel)
        {
        }

        /// <summary> Called when the panel becomes visible. </summary>
        /// <param name="panel"> The panel. </param>
        protected virtual void OnShow(VisualElement panel)
        {
        }

        /// <summary> Called when the panel becomes hidden. </summary>
        /// <param name="panel"> The panel. </param>
        protected virtual void OnHide(VisualElement panel)
        {
        }

        /// <summary> Disable this UI. </summary>
        protected void DisableUI()
        {
            var uiState = SystemAPI.GetSingletonRW<UIState>();
            uiState.ValueRW.Value[this.stateKey] = false;
        }

        /// <summary>
        /// Override to change how the add/remove behaviour of adding to the parent works.
        /// If you override this, you must also override RemoveFromParent.
        /// </summary>
        /// <param name="panel"> The panel element. </param>
        /// <param name="priority"> The panel priority. </param>
        private void AddToParent(VisualElement panel, int priority)
        {
            UIDocumentManager.Instance.AddPanel(panel, priority);
        }

        /// <summary>
        /// Override to change how the add/remove behaviour of adding to the parent works.
        /// If you override this, you must also override AddToParent.
        /// </summary>
        /// <param name="panel"> The panel element. </param>
        private void RemoveFromParent(VisualElement panel)
        {
            UIDocumentManager.Instance.RemovePanel(panel);
        }

#if UNITY_EDITOR
        private void OnEditorRebuild()
        {
            this.OnStopRunning();
            this.initialized = false;
            this.OnStartRunning();
        }
#endif
    }
}
#endif
