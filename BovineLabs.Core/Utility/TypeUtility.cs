namespace BovineLabs.Core.Utility
{
    using System;

    public static class TypeUtility
    {
        public static bool MatchesOpenGeneric(Type type, Type openGeneric)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
                {
                    return true;
                }

                // Proceed up the inheritance chain.
                type = type.BaseType;
            }

            return false;
        }

        public static bool GetOpenGenericArgumentType(Type type, Type openGeneric, out Type dataType)
        {
            dataType = null;
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric)
                {
                    // Found it; extract and return the type argument T.
                    dataType = type.GetGenericArguments()[0];
                    return true;
                }

                // Proceed up the inheritance chain.
                type = type.BaseType;
            }

            return false;
        }
    }
}
