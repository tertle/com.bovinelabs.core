namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities.Serialization;
    using Unity.Entities.UI;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    using InspectorUtility = BovineLabs.Core.Editor.Internal.InspectorUtility;

    internal class EntitySceneReferenceInspector : PropertyInspector<EntitySceneReference>
    {
        private PropertyElement _idField;
        private ObjectField _objectField;
        private Foldout _field;

        public override VisualElement Build()
        {
            _field = new Foldout { value = false };

            _idField = PropertyElement.MakeWithValue(Target.Id);
            _idField.SetEnabled(false);
            InspectorUtility.AddRuntimeBar(_idField);

            _objectField = new ObjectField { enabledSelf = !IsReadOnly };

            InspectorUtility.AddRuntimeBar(_objectField);

            _field.Add(_idField);
            _field.Add(_objectField);

            Update();

            _objectField.RegisterValueChangedCallback(evt =>
            {
                Target = new EntitySceneReference((SceneAsset)evt.newValue);
            });

            // this.objectField.RegisterCallback<GeometryChangedEvent, VisualElement>((_, f) => StylingUtility.AlignInspectorLabelWidth(f), this.objectField);

            return _field;
        }

        public override void Update()
        {
            var target = Target;
            var sceneAsset = GetSceneAsset(target);

            _idField!.SetTarget(target.Id);
            _objectField!.value = sceneAsset;

            _field!.text = sceneAsset == null ? DisplayName : $"{DisplayName} : {sceneAsset.name}";
        }

        private SceneAsset GetSceneAsset(EntitySceneReference sceneReference)
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

