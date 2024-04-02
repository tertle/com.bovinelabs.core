// <copyright file="SubSceneLoadConfigEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.SubScenes;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Custom editor for <see cref="SubSceneLoadConfig" />. </summary>
    [CustomEditor(typeof(SubSceneLoadConfig))]
    public class SubSceneLoadConfigEditor : ElementEditor
    {
        private SerializedProperty? loadMode;

        private PropertyField? isRequired;
        private PropertyField? loadMaxDistanceOverride;
        private PropertyField? unloadMaxDistanceOverride;

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "loadMode":
                    this.loadMode = property;
                    var field = CreatePropertyField(property);
                    field.RegisterValueChangeCallback(this.LoadModeChanged);
                    return field;

                case "isRequired":
                    this.isRequired = CreatePropertyField(property);
                    return this.isRequired;

                case "loadMaxDistanceOverride":
                    this.loadMaxDistanceOverride = CreatePropertyField(property);
                    return this.loadMaxDistanceOverride;

                case "unloadMaxDistanceOverride":
                    this.unloadMaxDistanceOverride = CreatePropertyField(property);
                    return this.unloadMaxDistanceOverride;
            }

            return CreatePropertyField(property);
        }

        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.Rebuild();
        }

        private void LoadModeChanged(SerializedPropertyChangeEvent evt)
        {
            this.Rebuild();
        }

        private void Rebuild()
        {
            SetVisible(this.isRequired!, false);
            SetVisible(this.loadMaxDistanceOverride!, false);
            SetVisible(this.unloadMaxDistanceOverride!, false);

            var loadModeIndex = this.loadMode!.enumValueIndex;
            switch ((SubSceneLoadMode)loadModeIndex)
            {
                case SubSceneLoadMode.AutoLoad:
                    SetVisible(this.isRequired!, true);
                    break;
                case SubSceneLoadMode.BoundingVolume:
                    SetVisible(this.loadMaxDistanceOverride!, true);
                    SetVisible(this.unloadMaxDistanceOverride!, true);
                    break;
                case SubSceneLoadMode.OnDemand:
                    break;
            }
        }
    }
}
#endif
