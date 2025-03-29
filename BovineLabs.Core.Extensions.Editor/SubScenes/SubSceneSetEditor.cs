// <copyright file="SubSceneSetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(SubSceneSet))]
    public class SubSceneSetEditor : ElementEditor
    {
        private SerializedProperty? isRequired;

        private PropertyField? waitForLoadField;
        private PropertyField? isRequiredField;
        private PropertyField? autoLoadField;

        /// <inheritdoc />
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case nameof(SubSceneSet.IsRequired):
                    this.isRequired = property;
                    return this.isRequiredField = CreatePropertyField(property);

                case nameof(SubSceneSet.WaitForLoad):
                    return this.waitForLoadField = CreatePropertyField(property);

                case nameof(SubSceneSet.AutoLoad):
                    return this.autoLoadField = CreatePropertyField(property);

                default:
                    return base.CreateElement(property);
            }
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.UpdateFields(this.isRequired!);

            this.isRequiredField!.RegisterValueChangeCallback(evt => this.UpdateFields(evt.changedProperty));
        }

        private void UpdateFields(SerializedProperty property)
        {
            var value = property.boolValue;

            ElementUtility.SetVisible(this.waitForLoadField!, !value);
            ElementUtility.SetVisible(this.autoLoadField!, !value);
        }
    }
}
#endif