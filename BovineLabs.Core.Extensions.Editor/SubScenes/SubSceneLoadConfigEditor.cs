// <copyright file="SubSceneLoadConfigEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SubScenes
{
    using System;
    using BovineLabs.Core.SubScenes;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Custom editor for <see cref="SubSceneLoadConfig" />. </summary>
    [CustomEditor(typeof(SubSceneLoadConfig))]
    public class SubSceneLoadConfigEditor : Editor
    {
        private Toggle autoLoadInspector;
        private VisualElement boundingVolumeInspector;
        private VisualElement customDrawers;
        private VisualElement inspector;

        private SerializedProperty isRequired;
        private SerializedProperty loadMaxDistanceOverride;
        private SerializedProperty loadMode;
        private SerializedProperty targetWorld;
        private SerializedProperty unloadMaxDistanceOverride;

        internal void OnEnable()
        {
            this.targetWorld = this.serializedObject.FindProperty("targetWorld");
            this.loadMode = this.serializedObject.FindProperty("loadMode");
            this.isRequired = this.serializedObject.FindProperty("isRequired");
            this.loadMaxDistanceOverride = this.serializedObject.FindProperty("loadMaxDistanceOverride");
            this.unloadMaxDistanceOverride = this.serializedObject.FindProperty("unloadMaxDistanceOverride");
        }

        public override VisualElement CreateInspectorGUI()
        {
            this.inspector = new VisualElement();

            // Properties that always exist
            this.inspector.Add(new PropertyField(this.targetWorld));
            var loadModeProperty = new PropertyField(this.loadMode);
            this.inspector.Add(loadModeProperty);
            loadModeProperty.RegisterValueChangeCallback(_ => this.Rebuild());

            this.customDrawers = new VisualElement();
            this.inspector.Add(this.customDrawers);

            // NOTE: can not for the life of me get UI Toolkit to render this properly if it isn't selected at start so just using regular fields
            this.autoLoadInspector = new Toggle(this.isRequired.displayName) { value = this.isRequired.boolValue };
            this.autoLoadInspector.RegisterValueChangedCallback(evt =>
            {
                this.isRequired.boolValue = evt.newValue;
                this.serializedObject.ApplyModifiedProperties();
            });

            var loadDistanceFoldout = new Foldout
            {
                text = "Load Distance Override",
                value = this.loadMaxDistanceOverride.floatValue > 0,
            };

            var load = new FloatField(this.loadMaxDistanceOverride.displayName) { value = this.loadMaxDistanceOverride.floatValue };
            var unload = new FloatField(this.unloadMaxDistanceOverride.displayName) { value = this.unloadMaxDistanceOverride.floatValue };
            load.RegisterValueChangedCallback(evt =>
            {
                this.loadMaxDistanceOverride.floatValue = evt.newValue;
                this.serializedObject.ApplyModifiedProperties();
            });
            unload.RegisterValueChangedCallback(evt =>
            {
                this.unloadMaxDistanceOverride.floatValue = evt.newValue;
                this.serializedObject.ApplyModifiedProperties();
            });

            loadDistanceFoldout.Add(load);
            loadDistanceFoldout.Add(unload);

            this.boundingVolumeInspector = loadDistanceFoldout;

            this.Rebuild();

            return this.inspector;
        }

        private void Rebuild()
        {
            if (this.autoLoadInspector.parent != null)
            {
                this.customDrawers.Remove(this.autoLoadInspector);
            }

            if (this.boundingVolumeInspector.parent != null)
            {
                this.customDrawers.Remove(this.boundingVolumeInspector);
            }

            var loadModeIndex = this.loadMode.enumValueIndex;
            switch ((SubSceneLoadMode)loadModeIndex)
            {
                case SubSceneLoadMode.AutoLoad:
                    this.customDrawers.Add(this.autoLoadInspector);
                    break;
                case SubSceneLoadMode.BoundingVolume:
                    this.customDrawers.Add(this.boundingVolumeInspector);
                    break;
                case SubSceneLoadMode.OnDemand:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
