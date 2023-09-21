// <copyright file="LocalizationToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && UNITY_LOCALIZATION
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using BovineLabs.Core.Debug.Toolbar;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial class LocalizationToolbarSystem : ToolbarSystemBase
    {
        private VisualTreeAsset? asset;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Name => "Localization";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            this.asset = Resources.Load<VisualTreeAsset>("LocalizationGroup")!;
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
        }
    }
}
#endif
