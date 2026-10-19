namespace BovineLabs.Core.Editor.Overlays
{
    using UnityEditor;
    using UnityEditor.Overlays;
    using UnityEngine;
    using UnityEngine.UIElements;

    [Overlay(typeof(SceneView), "BovineLabs/SceneCameraPosition", "Scene Camera", defaultDisplay = true)]
    [Icon("SceneViewCamera")]
    public sealed class SceneCameraPositionOverlay : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var sceneView = (SceneView)containerWindow;
            var root = new VisualElement();
            root.style.minWidth = 320;
            root.SetEnabled(false);
            var positionField = CreateVectorField("Position", "Scene camera world position. Changes apply immediately.");
            var rotationField = CreateVectorField("Rotation", "Scene camera Euler angles in degrees. Rotation is fixed in 2D mode.");
            root.Add(positionField);
            root.Add(rotationField);

            positionField.RegisterValueChangedCallback(evt =>
            {
                var position = evt.newValue;
                if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
                {
                    positionField.SetValueWithoutNotify(sceneView.camera.transform.position);
                    return;
                }

                var cameraTransform = sceneView.camera.transform;
                // SceneView stores an orbit pivot, so retain the camera's offset from that pivot.
                var pivot = position + (cameraTransform.forward * sceneView.cameraDistance);
                sceneView.LookAt(pivot, cameraTransform.rotation, sceneView.size, sceneView.orthographic, true);
                sceneView.Repaint();
            });

            rotationField.RegisterValueChangedCallback(evt =>
            {
                var angles = evt.newValue;
                if (!float.IsFinite(angles.x) || !float.IsFinite(angles.y) || !float.IsFinite(angles.z))
                {
                    rotationField.SetValueWithoutNotify(sceneView.camera.transform.eulerAngles);
                    return;
                }

                var rotation = Quaternion.Euler(angles);
                // Move the orbit pivot with the new orientation to rotate the camera in place.
                var pivot = sceneView.camera.transform.position + (rotation * Vector3.forward * sceneView.cameraDistance);
                sceneView.LookAt(pivot, rotation, sceneView.size, sceneView.orthographic, true);
                sceneView.Repaint();
            });

            root.schedule.Execute(() =>
            {
                // Overlays can be created before the SceneView has initialized its camera.
                var camera = sceneView.camera;
                root.SetEnabled(camera != null);
                if (camera == null)
                {
                    return;
                }

                rotationField.SetEnabled(!sceneView.in2DMode);
                var focusedElement = root.focusController?.focusedElement as VisualElement;
                if (focusedElement == null || !positionField.Contains(focusedElement))
                {
                    positionField.SetValueWithoutNotify(camera.transform.position);
                }

                if (focusedElement == null || !rotationField.Contains(focusedElement))
                {
                    rotationField.SetValueWithoutNotify(camera.transform.eulerAngles);
                }
            }).Every(50);

            return root;
        }

        private static Vector3Field CreateVectorField(string label, string tooltip)
        {
            var field = new Vector3Field(label)
            {
                tooltip = tooltip,
            };

            field.labelElement.style.minWidth = 55;
            field.labelElement.style.width = 55;
            return field;
        }
    }
}
