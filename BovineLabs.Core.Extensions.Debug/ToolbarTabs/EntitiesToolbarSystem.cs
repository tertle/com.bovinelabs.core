// <copyright file="EntitiesToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using BovineLabs.Core.Debug.Toolbar;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The toolbar for monitoring the number of entities, chunks and archetypes of a world. </summary>
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial class EntitiesToolbarSystem : ToolbarSystemBase
    {
        private VisualTreeAsset? asset;
        private Label? archetypes;
        private Label? chunks;
        private Label? entities;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Name => "Entities";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            this.asset = Resources.Load<VisualTreeAsset>("EntitiesGroup");
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
            this.entities = element.Q<Label>("entities");
            this.archetypes = element.Q<Label>("archetypes");
            this.chunks = element.Q<Label>("chunks");
        }

        /// <inheritdoc />
        protected override void OnUpdateVisible()
        {
            this.entities!.text = this.EntityManager.UniversalQuery.CalculateEntityCountWithoutFiltering().ToString();
            this.archetypes!.text = this.EntityManager.NumberOfArchetype().ToString();
            this.chunks!.text = this.EntityManager.UniversalQuery.CalculateChunkCountWithoutFiltering().ToString();
        }
    }
}
#endif
