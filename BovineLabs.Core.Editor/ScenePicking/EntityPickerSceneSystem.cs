// <copyright file="EntityPickerSceneSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_RENDERER
namespace BovineLabs.Core.Editor.ScenePicking
{
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;
    using UnityEngine;

    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class EntityPickerSceneSystem : SystemBase
    {
        private EntityPicker entityPicker;

        public void OnScene(SceneView sceneView)
        {
            var e = Event.current;
            var control = GUIUtility.GetControlID(FocusType.Passive);

            if (e.type != EventType.MouseDown || e.button != 0)
            {
                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }

                return;
            }

            this.entityPicker.CompleteDependency();
            var hit = this.entityPicker.Pick(e.mousePosition, sceneView.camera, default);

            if (hit != Entity.Null)
            {
                EntitySelectionProxy.SelectEntity(this.World, hit);
                GUIUtility.hotControl = control;
                e.Use();
            }
        }

        protected override void OnCreate()
        {
            this.entityPicker = new EntityPicker(this);
            this.Enabled = false;
        }

        protected override void OnDestroy()
        {
            this.entityPicker.Dispose();
        }

        protected override void OnUpdate()
        {
            // NO - OP
        }
    }
}
#endif
