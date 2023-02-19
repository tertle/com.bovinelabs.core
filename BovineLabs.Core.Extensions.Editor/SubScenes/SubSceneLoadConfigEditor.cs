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
        private VisualElement autoLoadInspector;
        private VisualElement boundingVolumeInspector;

        private VisualElement customDrawers;

        private VisualElement inspector;
        private SerializedProperty isRequired;
        private SerializedProperty loadMaxDistanceOverride;
        private SerializedProperty loadMode;
        private VisualElement onDemandInspector;
        private SerializedProperty subSceneID;
        private SerializedProperty targetWorld;
        private SerializedProperty unloadMaxDistanceOverride;

        private SubSceneLoadConfig SubSceneLoadConfig => (SubSceneLoadConfig)this.target;

        internal void OnEnable()
        {
            this.targetWorld = this.serializedObject.FindProperty("targetWorld");
            this.loadMode = this.serializedObject.FindProperty("loadMode");
            this.isRequired = this.serializedObject.FindProperty("isRequired");
            this.loadMaxDistanceOverride = this.serializedObject.FindProperty("loadMaxDistanceOverride");
            this.unloadMaxDistanceOverride = this.serializedObject.FindProperty("unloadMaxDistanceOverride");
            this.subSceneID = this.serializedObject.FindProperty("subSceneID");
        }

        public override VisualElement CreateInspectorGUI()
        {
            this.inspector = new VisualElement();

            // Properties that always exist
            this.inspector.Add(new PropertyField(this.targetWorld));
            var loadModeProperty = new PropertyField(this.loadMode);
            this.inspector.Add(loadModeProperty);
            loadModeProperty.RegisterValueChangeCallback(evt => this.Rebuild());

            this.customDrawers = new VisualElement();
            this.inspector.Add(this.customDrawers);

            this.autoLoadInspector = new VisualElement();
            this.autoLoadInspector.Add(new PropertyField(this.isRequired));

            var loadDistanceFoldout = new Foldout
            {
                text = "Load Distance Override",
                value = this.loadMaxDistanceOverride.floatValue > 0,
            };
            loadDistanceFoldout.Add(new PropertyField(this.loadMaxDistanceOverride));
            loadDistanceFoldout.Add(new PropertyField(this.unloadMaxDistanceOverride));
            this.boundingVolumeInspector = loadDistanceFoldout;

            this.onDemandInspector = new VisualElement();
            this.onDemandInspector.Add(new PropertyField(this.subSceneID));

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

            if (this.onDemandInspector.parent != null)
            {
                this.customDrawers.Remove(this.onDemandInspector);
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
                    this.customDrawers.Add(this.onDemandInspector);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
