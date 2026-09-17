namespace BovineLabs.Core.Editor.Inspectors
{
    using UnityEditor;
    using UnityEditor.UIElements;

    public static class PropertyUtil
    {
        public static PropertyField CreateProperty(SerializedProperty property, SerializedObject serializedObject)
        {
            var field = new PropertyField(property) { name = "PropertyField:" + property?.propertyPath };
            field.Bind(serializedObject);
            return field;
        }

        public static PropertyField CreateProperty(SerializedProperty property)
        {
            var field = new PropertyField(property) { name = "PropertyField:" + property.propertyPath };
            field.Bind(property.serializedObject);
            return field;
        }
    }
}
