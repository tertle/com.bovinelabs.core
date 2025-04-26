// <copyright file="ReflectionUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Unity;
    using Unity.Collections;
    using Unity.Profiling;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.Compilation;
    using Assembly = UnityEditor.Compilation.Assembly;
    using AssemblyFlags = UnityEditor.Compilation.AssemblyFlags;
#endif

    /// <summary> Common reflection helpers. </summary>
    public static class ReflectionUtility
    {
        private static readonly Dictionary<System.Reflection.Assembly, Type[]> AssemblyTypes = new();
        private static readonly Dictionary<System.Reflection.Assembly, Type[]> AssemblyNonGenericTypes = new();
        private static readonly Dictionary<System.Reflection.Assembly, MethodInfo[]> AssemblyMethods = new();

        private static System.Reflection.Assembly[] allAssemblies;
        private static Type[] allTypes;
        private static Type[] allTypesWithImplementation;
        private static Type[] allTypesWithImplementationNoGeneric;
        private static MethodInfo[] allMethods;

#if UNITY_EDITOR
        private static Dictionary<string, Assembly> assembliesMap;
#endif

        public static System.Reflection.Assembly[] AllAssemblies => allAssemblies ??= AppDomain.CurrentDomain.GetAssemblies();

        public static Type[] AllTypes => allTypes ??= AllAssemblies.SelectMany(GetTypes).ToArray();

        private static Type[] AllTypesWithImplementation => allTypesWithImplementation ??= AllTypes.Where(t => !t.IsAbstract && !t.IsInterface).ToArray();

        private static Type[] AllTypesWithImplementationNoGeneric =>
            allTypesWithImplementationNoGeneric ??= AllTypesWithImplementation.Where(t => !t.ContainsGenericParameters).ToArray();

#if UNITY_EDITOR
        private static Dictionary<string, Assembly> AssembliesMap => assembliesMap ??= CompilationPipeline.GetAssemblies().ToDictionary(r => r.name, r => r);

#endif

        public static Type[] GetTypes(System.Reflection.Assembly assembly)
        {
            if (!AssemblyTypes.TryGetValue(assembly, out var types))
            {
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.LogWarning($"Unable to load types for assembly {assembly.FullName}");
                    types = Array.Empty<Type>();
                }

                AssemblyTypes[assembly] = types;
            }

            return types;
        }

        public static Type[] GetNonGenericTypes(System.Reflection.Assembly assembly)
        {
            if (!AssemblyNonGenericTypes.TryGetValue(assembly, out var types))
            {
                AssemblyNonGenericTypes[assembly] = types = GetTypes(assembly).Where(t => !t.ContainsGenericParameters).ToArray();
            }

            return types;
        }

        public static MethodInfo[] GetMethods(System.Reflection.Assembly assembly)
        {
            if (!AssemblyMethods.TryGetValue(assembly, out var methods))
            {
                AssemblyMethods[assembly] = methods = GetTypes(assembly)
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                    .ToArray();
            }

            return methods;
        }

        /// <summary> Searches all assemblies to find all types that implement a type. </summary>
        /// <typeparam name="T"> The base type that is inherited from. </typeparam>
        /// <returns> All the types. </returns>
        public static IEnumerable<T> GetAllAssemblyAttributes<T>()
            where T : Attribute
        {
            return AllAssemblies.SelectMany(s => s.GetCustomAttributes(typeof(T), true)).Cast<T>();
        }

        /// <summary> Finds an implementation of an interface T in the all assemblies, falling back on TD if no others found. </summary>
        /// <typeparam name="T"> The interface to fall back on. </typeparam>
        /// <typeparam name="TD"> The type of the default implementation. </typeparam>
        /// <returns> The implementation found. null if default is not set and none were found. </returns>
        /// <exception cref="ArgumentException"> Type is valid. </exception>
        /// <exception cref="InvalidOperationException"> Implementation not found. </exception>
        public static T GetCustomImplementation<T, TD>()
            where TD : T
        {
            return GetCustomImplementation<T>(typeof(TD));
        }

        /// <summary> Finds an implementation of an interface T in the all assemblies. </summary>
        /// <typeparam name="T"> The interface to fall back on. </typeparam>
        /// <returns> The implementation found. null if default is not set and none were found. </returns>
        /// <exception cref="ArgumentException"> Type is valid. </exception>
        /// <exception cref="InvalidOperationException"> Implementation not found. </exception>
        public static T GetCustomImplementation<T>()
        {
            return GetCustomImplementation<T>(null);
        }

        /// <summary>
        /// Gets all types that inherit and have a generic definition of itself.
        /// class SettingsBase{T} : where T : SettingsBase{T} for example.
        /// </summary>
        /// <param name="type"> The generic type definitions. </param>
        /// <returns> An enumeration of the types. </returns>
        public static IEnumerable<Type> GetAllWithGenericDefinition(Type type)
        {
            var types = from t in AllTypesWithImplementation
                let i = t.BaseType
                where i is { IsGenericType: true } && i.GetGenericTypeDefinition() == type && i.GetGenericArguments()[0] == t // must equal itself
                select t;

            return types;
        }

        /// <summary> Searches all assemblies to find all types that implement a type. </summary>
        /// <param name="type"> The base type that is inherited from. </param>
        /// <param name="includeGenerics"> Determines if generic types should also be returned. </param>
        /// <returns> All the types. </returns>
        public static IEnumerable<Type> GetAllImplementations(Type type, bool includeGenerics = false)
        {
            var coreAssembly = type.Assembly;

            if (includeGenerics)
            {
                return AllAssemblies
                    .Where(asm => asm.IsAssemblyReferencingAssembly(coreAssembly))
                    .SelectMany(asm => GetNonGenericTypes(asm).Where(t => !t.IsAbstract && !t.IsInterface && type.IsAssignableFrom(t)));
            }

            return AllAssemblies
                .Where(asm => asm.IsAssemblyReferencingAssembly(coreAssembly))
                .SelectMany(asm => GetTypes(asm).Where(t => !t.IsAbstract && !t.IsInterface && type.IsAssignableFrom(t)));
        }

        /// <summary> Searches all assemblies to find all types that implement a type. </summary>
        /// <typeparam name="T"> The base type that is inherited from. </typeparam>
        /// <param name="includeGenerics"> Determines if generic types should also be returned. </param>
        /// <returns> All the types. </returns>
        public static IEnumerable<Type> GetAllImplementations<T>(bool includeGenerics = false)
        {
            return GetAllImplementations(typeof(T), includeGenerics);
        }

        public static IEnumerable<Type> GetAllOpenGenericImplementations(Type type)
        {
            return AllTypesWithImplementation.Where(s =>
            {
                var baseType = s.BaseType;
                return (baseType is { IsGenericType: true } && type.IsAssignableFrom(baseType.GetGenericTypeDefinition())) ||
                    s.GetInterfaces().Any(z => z.IsGenericType && type.IsAssignableFrom(z.GetGenericTypeDefinition()));
            });
        }

        /// <summary> Searches all assemblies to find all types that have an attribute. </summary>
        /// <typeparam name="T"> The attribute to search for. </typeparam>
        /// <returns> All the types. </returns>
        public static IEnumerable<Type> GetAllWithAttribute<T>()
            where T : Attribute
        {
// #if UNITY_EDITOR
//             return TypeCache.GetTypesWithAttribute<T>();
// #else
            var attributeType = typeof(T);
            var coreAssembly = attributeType.Assembly;

            foreach (var assembly in AllAssemblies)
            {
                if (!assembly.IsAssemblyReferencingAssembly(coreAssembly))
                {
                    continue;
                }

                foreach (var type in GetNonGenericTypes(assembly))
                {
                    if (!type.IsDefined(attributeType, true))
                    {
                        continue;
                    }

                    yield return type;
                }
            }
// #endif
        }

        public static IEnumerable<MethodInfo> GetMethodsWithAttribute<T>()
            where T : Attribute
        {
#if UNITY_EDITOR
            using (new ProfilerMarker("GetMethodsWithAttribute").Auto())
            {
                return TypeCache.GetMethodsWithAttribute<T>();
            }
#else
            var attributeType = typeof(T);
            var coreAssembly = attributeType.Assembly;

            foreach (var assembly in AllAssemblies)
            {
                if (!assembly.IsAssemblyReferencingAssembly(coreAssembly))
                {
                    continue;
                }

                foreach (var type in GetMethods(assembly))
                {
                    if (!type.IsDefined(attributeType, false))
                    {
                        continue;
                    }

                    yield return type;
                }
            }
#endif
        }

        public static IEnumerable<(MethodInfo Method, T Attribute)> GetMethodsAndAttribute<T>()
            where T : Attribute
        {
            var attributeType = typeof(T);
            var coreAssembly = attributeType.Assembly;

            foreach (var assembly in AllAssemblies)
            {
                if (!assembly.IsAssemblyReferencingAssembly(coreAssembly))
                {
                    continue;
                }

                foreach (var type in GetMethods(assembly))
                {
                    var attribute = type.GetCustomAttribute<T>();

                    if (attribute == null)
                    {
                        continue;
                    }

                    yield return (type, attribute);
                }
            }
        }

        /// <summary> Searches all assemblies to find all types that implement both 2 types. </summary>
        /// <typeparam name="T1"> The first base type that is inherited from. </typeparam>
        /// <typeparam name="T2"> The second base type that is inherited from. </typeparam>
        /// <returns> All the types that inherit both types. </returns>
        public static IEnumerable<Type> GetAllImplementations<T1, T2>()
            where T1 : class
            where T2 : class
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);

            return AllTypesWithImplementationNoGeneric.Where(t => t != type1 && t != type2).Where(t => type1.IsAssignableFrom(t) && type2.IsAssignableFrom(t));
        }

        /// <summary> Searches all assemblies to find all types that implement both 2 types but only keep top level inheritance. </summary>
        /// <typeparam name="T1"> The first base type that is inherited from. </typeparam>
        /// <typeparam name="T2"> The second base type that is inherited from. </typeparam>
        /// <returns> All the types that inherit both types. </returns>
        public static IEnumerable<Type> GetAllImplementationsRootOnly<T1, T2>()
            where T1 : class
            where T2 : class
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);

            var all = AllTypesWithImplementationNoGeneric
                .Where(t => t != type1 && t != type2)
                .Where(t => type1.IsAssignableFrom(t) && type2.IsAssignableFrom(t))
                .ToList();

            // Remove anything that has been been inherited from
            for (var i = all.Count - 1; i >= 0; i--)
            {
                var testing = all[i];
                if (all.Any(t => t != testing && testing.IsAssignableFrom(t)))
                {
                    all.RemoveAtSwapBack(i);
                }
            }

            return all;
        }

        /// <summary> Checks if an assembly is referencing another assembly. the name of all assemblies with a specific reference. </summary>
        /// <param name="assembly"> The assembly to check. </param>
        /// <param name="reference"> The reference to check if the assembly has. </param>
        /// <returns> True if referencing. </returns>
        public static bool IsAssemblyReferencingAssembly(this System.Reflection.Assembly assembly, System.Reflection.Assembly reference)
        {
            if (assembly == reference)
            {
                return true;
            }

            var referenceName = reference.GetName().Name;
            return assembly.GetReferencedAssemblies().Any(referenced => referenced.Name == referenceName);
        }

        /// <summary> Gets the name of all assemblies with a specific reference. </summary>
        /// <param name="reference"> The reference. </param>
        /// <returns> The name of all the assemblies. </returns>
        public static IEnumerable<System.Reflection.Assembly> GetAllAssemblyWithReference(System.Reflection.Assembly reference)
        {
            return AllAssemblies.Where(a => IsAssemblyReferencingAssembly(a, reference));
        }

        public static FieldInfo GetFieldInBase(this Type type, string name)
        {
            while (true)
            {
                if (type == null)
                {
                    return null;
                }

                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly;

                var field = type.GetField(name, flags);
                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }
        }

#if UNITY_EDITOR

        /// <summary> Checks if an assembly is an editor only assembly. </summary>
        /// <param name="asm"> The assembly to check. </param>
        /// <returns> True if editor assembly. </returns>
        public static bool IsAssemblyEditorAssembly(this System.Reflection.Assembly asm)
        {
            if (!AssembliesMap.TryGetValue(asm.GetName().Name, out var uAssembly))
            {
                return false; // this happens in sub scene conversion process if a new assembly is added after loading unity...
            }

            return (uAssembly.flags & AssemblyFlags.EditorAssembly) != 0;
        }

        /// <summary> Checks if an assembly is an editor only assembly. </summary>
        /// <param name="asm"> The assembly to check. </param>
        /// <returns> True if editor assembly. </returns>
        public static bool IsTestEditorAssembly(this System.Reflection.Assembly asm)
        {
            if (!AssembliesMap.TryGetValue(asm.GetName().Name, out var uAssembly))
            {
                return false; // this happens in sub scene conversion process if a new assembly is added after loading unity...
            }

            return uAssembly.assemblyReferences.Any(c => c.name == "UnityEngine.TestRunner");
        }
#endif

        private static T GetCustomImplementation<T>(Type defaultImplementation)
        {
            var type = typeof(T);
            if (!type.IsInterface)
            {
                throw new ArgumentException("T should be an interface.", nameof(T));
            }

            var types = GetAllImplementations<T>().ToList();

            switch (types.Count)
            {
                case 0:
                    if (defaultImplementation != null)
                    {
                        throw new InvalidOperationException("Could not find default implementation");
                    }

                    // No implementation was found and default wasn't set
                    return default;
                case 1:
                    return (T)Activator.CreateInstance(types[0]);

                case 2:
                {
                    if (defaultImplementation != null)
                    {
                        if (types.Remove(defaultImplementation))
                        {
                            return (T)Activator.CreateInstance(types[0]);
                        }
                    }

                    break;
                }
            }

            throw new InvalidOperationException($"More than 1 implementation of {type} found");
        }
    }
}
