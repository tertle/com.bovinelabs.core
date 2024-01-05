// <copyright file="PhysicsToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && BL_DRAW && UNITY_PHYSICS
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.UI;
    using Unity.Properties;

    public class PhysicsToolbarBindings : IBindingObject<PhysicsToolbarBindings.Data>
    {
        private PhysicsToolbarBindings.Data data;

        public ref PhysicsToolbarBindings.Data Value => ref this.data;

        [CreateProperty]
        public bool DrawColliderEdges
        {
            get => this.data.DrawColliderEdges;
            set => this.data.DrawColliderEdges = value;
        }

        [CreateProperty]
        public bool DrawMeshColliderEdges
        {
            get => this.data.DrawMeshColliderEdges;
            set => this.data.DrawMeshColliderEdges = value;
        }

        [CreateProperty]
        public bool DrawColliderAabbs
        {
            get => this.data.DrawColliderAabbs;
            set => this.data.DrawColliderAabbs = value;
        }

        [CreateProperty]
        public bool DrawCollisionEvents
        {
            get => this.data.DrawCollisionEvents;
            set => this.data.DrawCollisionEvents = value;
        }

        [CreateProperty]
        public bool DrawTriggerEvents
        {
            get => this.data.DrawTriggerEvents;
            set => this.data.DrawTriggerEvents = value;
        }

        public struct Data
        {
            public bool DrawColliderEdges;
            public bool DrawMeshColliderEdges;
            public bool DrawColliderAabbs;
            public bool DrawCollisionEvents;
            public bool DrawTriggerEvents;
        }
    }
}
#endif
