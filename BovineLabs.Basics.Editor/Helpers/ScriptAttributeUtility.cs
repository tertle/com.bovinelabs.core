namespace BovineLabs.Basics.Editor.Helpers
{
    using System;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEditor;

    public static class ScriptAttributeUtility
    {
        // TODO CACHE
        // https://github.com/Unity-Technologies/UnityCsReference/blob/fbd4f2bd409f7adb9b077acfaed620bf992f7e55/Editor/Mono/ScriptAttributeGUI/ScriptAttributeUtility.cs#L447-L472
        internal static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type)
        {
            var fieldInfo = GetFieldInfoAndStaticTypeFromProperty(property, out type);
            if (fieldInfo == null)
            {
                return null;
            }

            // // Managed references are a special case, we need to override the static type
            // // returned by 'GetFieldInfoFromPropertyPath' for custom property handler matching
            // // by the dynamic type of the instance.
            // if (property.propertyType == SerializedPropertyType.ManagedReference)
            // {
            //     Type managedReferenceInstanceType;
            //
            //     // Try to get a Type instance for the managed reference
            //     if (GetTypeFromManagedReferenceFullTypeName(property.managedReferenceFullTypename, out managedReferenceInstanceType))
            //     {
            //         type = managedReferenceInstanceType;
            //     }
            //
            //     // We keep the fallback to the field type returned by 'GetFieldInfoFromPropertyPath'.
            // }

            return fieldInfo;
        }

        /// <summary>
        /// Returns the field info and field type for the property. The types are based on the
        /// static field definition.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
        {
            var classType = GetScriptTypeFromProperty(property);
            if (classType == null)
            {
                type = null;
                return null;
            }

            var fieldPath = property.propertyPath;
            // if (property.isReferencingAManagedReferenceField)
            // {
            //     // When the field we are trying to access is a dynamic instance, things are a bit more tricky
            //     // since we cannot "statically" (looking only at the parent class field types) know the actual
            //     // "classType" of the parent class.
            //
            //     // The issue also is that at this point our only view on the object is the very limited SerializedProperty.
            //
            //     // So we have to:
            //     // 1. try to get the FQN from for the current managed type from the serialized data,
            //     // 2. get the path *in the current managed instance* of the field we are pointing to,
            //     // 3. foward that to 'GetFieldInfoFromPropertyPath' as if it was a regular field,
            //
            //     var objectTypename = property.GetFullyQualifiedTypenameForCurrentTypeTreeInternal();
            //     GetTypeFromManagedReferenceFullTypeName(objectTypename, out classType);
            //
            //     fieldPath = property.GetPropertyPathInCurrentManagedTypeTreeInternal();
            // }

            if (classType == null)
            {
                type = null;
                return null;
            }

            return GetFieldInfoFromPropertyPath(classType, fieldPath, out type);
        }

        private static Type GetScriptTypeFromProperty(SerializedProperty property)
        {
            if (property.serializedObject.targetObject != null)
            {
                return property.serializedObject.targetObject.GetType();
            }

            // Fallback in case the targetObject has been destroyed but the property is still valid.
            SerializedProperty scriptProp = property.serializedObject.FindProperty("m_Script");

            if (scriptProp == null)
                return null;

            MonoScript script = scriptProp.objectReferenceValue as MonoScript;

            if (script == null)
                return null;

            return script.GetClass();
        }

         private static FieldInfo GetFieldInfoFromPropertyPath(Type host, string path, out Type type)
        {
            // Cache cache = new Cache(host, path);

            // FieldInfoCache fieldInfoCache = null;
            // if (s_FieldInfoFromPropertyPathCache.TryGetValue(cache, out fieldInfoCache))
            // {
            //     type = fieldInfoCache?.type;
            //     return fieldInfoCache?.fieldInfo;
            // }

            const string arrayData = @"\.Array\.data\[[0-9]+\]";
            // we are looking for array element only when the path ends with Array.data[x]
            var lookingForArrayElement = Regex.IsMatch(path, arrayData + "$");
            // remove any Array.data[x] from the path because it is prevents cache searching.
            path = Regex.Replace(path, arrayData, ".___ArrayElement___");

            FieldInfo fieldInfo = null;
            type = host;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string member = parts[i];
                // GetField on class A will not find private fields in base classes to A,
                // so we have to iterate through the base classes and look there too.
                // Private fields are relevant because they can still be shown in the Inspector,
                // and that applies to private fields in base classes too.
                FieldInfo foundField = null;
                for (Type currentType = type; foundField == null && currentType != null; currentType = currentType.BaseType)
                    foundField = currentType.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (foundField == null)
                {
                    type = null;
                    // s_FieldInfoFromPropertyPathCache.Add(cache, null);
                    return null;
                }

                fieldInfo = foundField;
                type = fieldInfo.FieldType;
                // TODO
                // we want to get the element type if we are looking for Array.data[x]
                // if (i < parts.Length - 1 && parts[i + 1] == "___ArrayElement___" && type.IsArrayOrList())
                // {
                //     i++; // skip the "___ArrayElement___" part
                //     type = type.GetArrayOrListElementType();
                // }
            }

            // TODO
            // we want to get the element type if we are looking for Array.data[x]
            // if (lookingForArrayElement && type != null && type.IsArrayOrList())
            // {
            //     type = type.GetArrayOrListElementType();
            // }

            // fieldInfoCache = new FieldInfoCache
            // {
            //     type = type,
            //     fieldInfo = fieldInfo
            // };
            // s_FieldInfoFromPropertyPathCache.Add(cache, fieldInfoCache);
            return fieldInfo;
        }
    }
}