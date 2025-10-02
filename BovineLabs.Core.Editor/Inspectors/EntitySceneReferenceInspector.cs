// <copyright file="EntitySceneReferenceInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities;
    using Unity.Entities.Editor;
    using Unity.Entities.Serialization;
    using Unity.Entities.UI;
    using Unity.Properties;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using InspectorUtility = BovineLabs.Core.Editor.Internal.InspectorUtility;

    internal class EntitySceneReferenceInspector : PropertyInspector<EntitySceneReference>
    {
        private PropertyElement? idField;
        private ObjectField? objectField;
        private Foldout? field;

        /// <inheritdoc/>
        public override VisualElement Build()
        {
            this.field = new Foldout { value = false };

            this.idField = PropertyElement.MakeWithValue(this.Target.Id);
            this.idField.SetEnabled(false);
            InspectorUtility.AddRuntimeBar(this.idField);

            this.objectField = new ObjectField { enabledSelf = !this.IsReadOnly };

            InspectorUtility.AddRuntimeBar(this.objectField);

            this.field.Add(this.idField);
            this.field.Add(this.objectField);

            this.Update();

            this.objectField.RegisterValueChangedCallback(evt =>
            {
                this.Target = new EntitySceneReference((SceneAsset)evt.newValue);
            });

            // this.objectField.RegisterCallback<GeometryChangedEvent, VisualElement>((_, f) => StylingUtility.AlignInspectorLabelWidth(f), this.objectField);

            return this.field;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            var target = this.Target;
            var sceneAsset = this.GetSceneAsset(target);

            this.idField!.SetTarget(target.Id);
            this.objectField!.value = sceneAsset;

            this.field!.text = sceneAsset == null ? this.DisplayName : $"{this.DisplayName} : {sceneAsset.name}";
        }

        private SceneAsset? GetSceneAsset(EntitySceneReference sceneReference)
        {
            if (!sceneReference.Id.IsValid)
            {
                return null;
            }

            if (sceneReference.Id.GenerationType != WeakReferenceGenerationType.EntityScene)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneReference.Id.GlobalId.AssetGUID));
        }
    }
}

