// <copyright file="PhysicsToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using BovineLabs.Core.Debug.PhysicsDrawers;
    using BovineLabs.Core.Debug.Toolbar;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial class PhysicsToolbarSystem : ToolbarSystemBase
    {
        private VisualTreeAsset? asset;
        private Toggle? broadphaseField;
        private Toggle? colliderAabbsField;
        private Toggle? colliderEdgesField;
        private Toggle? collidersField;
        private Toggle? collisionEventsField;
        private Toggle? contactsField;
        private Toggle? jointsField;
        private Toggle? massPropertiesField;
        private Toggle? triggerEventsField;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Name => "Physics";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            this.asset = Resources.Load<VisualTreeAsset>("PhysicsGroup");
            this.EntityManager.AddComponentData(this.SystemHandle, new PhysicsDebugDraw { DrawMeshColliderEdges = false }); // TODO
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
            this.collidersField = element.Q<Toggle>("colliders");
            this.colliderEdgesField = element.Q<Toggle>("collider-edges");
            this.colliderAabbsField = element.Q<Toggle>("collider-aabbs");
            this.broadphaseField = element.Q<Toggle>("broadphase");
            this.massPropertiesField = element.Q<Toggle>("mass-properties");
            this.contactsField = element.Q<Toggle>("contacts");
            this.collisionEventsField = element.Q<Toggle>("collision-events");
            this.triggerEventsField = element.Q<Toggle>("trigger-events");
            this.jointsField = element.Q<Toggle>("joints");

            // Currently not supported fields TODO REMOVE
            this.collidersField.RemoveFromHierarchy();
            this.broadphaseField.RemoveFromHierarchy();
            this.massPropertiesField.RemoveFromHierarchy();
            this.contactsField.RemoveFromHierarchy();
            this.jointsField.RemoveFromHierarchy();

            this.colliderEdgesField.RegisterValueChangedCallback(evt => { this.GetComponent().DrawColliderEdges = evt.newValue; });
            this.colliderAabbsField.RegisterValueChangedCallback(evt => { this.GetComponent().DrawColliderAabbs = evt.newValue; });
            this.collisionEventsField.RegisterValueChangedCallback(evt => { this.GetComponent().DrawCollisionEvents = evt.newValue; });
            this.triggerEventsField.RegisterValueChangedCallback(evt => { this.GetComponent().DrawTriggerEvents = evt.newValue; });
        }

        private ref PhysicsDebugDraw GetComponent()
        {
            return ref this.EntityManager.GetComponentDataRW<PhysicsDebugDraw>(this.SystemHandle).ValueRW;
        }
    }
}
#endif
