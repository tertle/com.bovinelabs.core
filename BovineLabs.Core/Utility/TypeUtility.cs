// <copyright file="TypeUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;

    public static class TypeUtility
    {
        /// <summary>
        /// Checks if a type matches an open generic.
        /// </summary>
        /// <param name="type">The type to check. </param>
        /// <param name="openGeneric"> The open generic to check against, must be something like typeof(SomeType&lt;&gt;). </param>
        /// <returns> True if it matches. </returns>
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

        /// <summary> Checks if a type matches an open generic and gets that generic argument. </summary>
        /// <param name="type">The type to check. </param>
        /// <param name="openGeneric"> The open generic to check against, must be something like typeof(SomeType&lt;&gt;). </param>
        /// <param name="dataType"> The generic argument to return. </param>
        /// <returns> True if it matches. </returns>
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
