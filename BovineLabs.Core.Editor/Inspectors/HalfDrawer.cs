// <copyright file="HalfDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(half))]
    public class HalfDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var valueProperty = property.FindPropertyRelative("value");

            var field = new FloatField(property.displayName) { isDelayed = true };
            field.SetValueWithoutNotify(new half { value = (ushort)valueProperty.intValue });

            field.RegisterValueChangedCallback(evt =>
            {
                valueProperty.intValue = new half(evt.newValue).value;
                property.serializedObject.ApplyModifiedProperties();
                field.SetValueWithoutNotify(new half { value = (ushort)valueProperty.intValue });
            });

            return field;
        }
    }

    [CustomPropertyDrawer(typeof(half2))]
    public class Half2Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var xProperty = property.FindPropertyRelative("x.value");
            var yProperty = property.FindPropertyRelative("y.value");

            var field = new Vector2Field(property.displayName);
            SetValue();

            field.RegisterValueChangedCallback(evt =>
            {
                xProperty.intValue = new half(evt.newValue.x).value;
                yProperty.intValue = new half(evt.newValue.y).value;
                property.serializedObject.ApplyModifiedProperties();

                // Write back the value to the element because the precision is going to be quite a bit different between half/float
                SetValue();
            });

            void SetValue()
            {
                field.SetValueWithoutNotify((float2)new half2
                {
                    x = new half { value = (ushort)xProperty.intValue },
                    y = new half { value = (ushort)yProperty.intValue },
                });
            }

            return field;
        }
    }

    [CustomPropertyDrawer(typeof(half3))]
    public class Half3Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var xProperty = property.FindPropertyRelative("x.value");
            var yProperty = property.FindPropertyRelative("y.value");
            var zProperty = property.FindPropertyRelative("z.value");

            var field = new Vector3Field(property.displayName);
            SetValue();

            field.RegisterValueChangedCallback(evt =>
            {
                xProperty.intValue = new half(evt.newValue.x).value;
                yProperty.intValue = new half(evt.newValue.y).value;
                zProperty.intValue = new half(evt.newValue.z).value;
                property.serializedObject.ApplyModifiedProperties();

                // Write back the value to the element because the precision is going to be quite a bit different between half/float
                SetValue();
            });

            void SetValue()
            {
                field.SetValueWithoutNotify((float3)new half3
                {
                    x = new half { value = (ushort)xProperty.intValue },
                    y = new half { value = (ushort)yProperty.intValue },
                    z = new half { value = (ushort)zProperty.intValue },
                });
            }

            return field;
        }
    }

    [CustomPropertyDrawer(typeof(half4))]
    public class Half4Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var xProperty = property.FindPropertyRelative("x.value");
            var yProperty = property.FindPropertyRelative("y.value");
            var zProperty = property.FindPropertyRelative("z.value");
            var wProperty = property.FindPropertyRelative("w.value");

            var field = new Vector4Field(property.displayName);
            SetValue();

            field.RegisterValueChangedCallback(evt =>
            {
                xProperty.intValue = new half(evt.newValue.x).value;
                yProperty.intValue = new half(evt.newValue.y).value;
                zProperty.intValue = new half(evt.newValue.z).value;
                wProperty.intValue = new half(evt.newValue.w).value;
                property.serializedObject.ApplyModifiedProperties();

                // Write back the value to the element because the precision is going to be quite a bit different between half/float
                SetValue();
            });

            void SetValue()
            {
                field.SetValueWithoutNotify((float4)new half4
                {
                    x = new half { value = (ushort)xProperty.intValue },
                    y = new half { value = (ushort)yProperty.intValue },
                    z = new half { value = (ushort)zProperty.intValue },
                });
            }

            return field;
        }
    }
}
