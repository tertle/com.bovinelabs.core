namespace BovineLabs.Testing
{
    // Helper class for reflection access to private fields
    public static class ReflectionTestHelper
    {
        public static void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}
