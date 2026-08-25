// <copyright file="BLIdProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(BLId))]
    public sealed class BLIdProperty : ElementProperty
    {
        /// <inheritdoc/>
        protected override bool IterateChildren => false;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            var idField = new IntegerField(nameof(BLId.ID)) { isReadOnly = true };
            var modField = new IntegerField(nameof(BLId.Mod)) { isReadOnly = true };

            var root = new VisualElement();
            root.Add(idField);
            root.Add(modField);

            UpdateFields(property, idField, modField);
            root.TrackPropertyValue(property, changedProperty => UpdateFields(changedProperty, idField, modField));

            return root;
        }

        private static void UpdateFields(SerializedProperty property, IntegerField idField, IntegerField modField)
        {
            var value = property.boxedValue is BLId id ? id : default;
            idField.SetValueWithoutNotify(value.ID);
            modField.SetValueWithoutNotify(value.Mod);
        }
    }
}
