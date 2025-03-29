// <copyright file="ConfigVarManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEngine;
    using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary> The manager for the config vars. Is pretty automated. </summary>
    public static class ConfigVarManager
    {
        private static readonly Regex ValidateNameRegex = new(@"^[a-z_+-][a-z0-9_+.-]*$");
        private static readonly Dictionary<ConfigVarAttribute, IConfigVarContainer> AllInternal = new();
        private static bool isInitialized;

        public static IReadOnlyDictionary<ConfigVarAttribute, IConfigVarContainer> All => AllInternal;

        public static IEnumerable<(ConfigVarAttribute ConfigVar, FieldInfo Field)> FindAllConfigVars()
        {
            foreach (var type in ReflectionUtility.GetAllWithAttribute<ConfigurableAttribute>())
            {
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    var configVar = field.GetCustomAttribute<ConfigVarAttribute>();

                    if (configVar == null)
                    {
                        continue;
                    }

                    yield return (configVar, field);
                }
            }
        }

        /// <summary> Initializes the <see cref="ConfigVarAttribute" />s throughout the project. </summary>

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        public static void Init()
        {
            if (isInitialized)
            {
                return;
            }

            Application.quitting += Shutdown;

            isInitialized = true;

            foreach (var (configVar, field) in FindAllConfigVars())
            {
                var fieldValue = field.GetValue(null);

                var container = GetContainer(fieldValue);

                if (container is NullConfigVarContainer)
                {
                    Debug.LogError($"ConfigVar on field ({field.Name} in {field.DeclaringType?.Name}) of type ({field.FieldType}) is not a supported type");
                    continue;
                }

                RegisterConfigVar(configVar, container);

                if (CommandLineArgs.TryGetArgument($"-{configVar.Name}", out var value))
                {
                    try
                    {
                        container.StringValue = value;
                        Debug.Log($"ConfigVar {configVar.Name} set from command line to {value}");
                    }
                    catch (Exception)
                    {
                        Debug.LogWarning(
                            $"Trying to set a configvar {configVar.Name} value of {value} which is not in the write type format. Faling back to default.");

                        container.StringValue = configVar.DefaultValue;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    container.StringValue = EditorPrefs.GetString(configVar.Name, configVar.DefaultValue);
#else
                    container.StringValue = configVar.DefaultValue;
#endif
                }
            }
        }

        private static void Shutdown()
        {
            if (!isInitialized)
            {
                return;
            }

#if UNITY_EDITOR
            foreach (var (configVar, container) in AllInternal)
            {
                EditorPrefs.SetString(configVar.Name, container.StringValue);
            }
#endif

            AllInternal.Clear();
            isInitialized = false;
        }

        private static void RegisterConfigVar(ConfigVarAttribute configVar, IConfigVarContainer container)
        {
            if (AllInternal.ContainsKey(configVar))
            {
                Debug.LogError($"Trying to register ConfigVar {configVar.Name} twice");
                return;
            }

            if (!ValidateNameRegex.IsMatch(configVar.Name))
            {
                Debug.LogError($"Trying to register ConfigVar with invalid name: {configVar.Name}");
                return;
            }

            AllInternal.Add(configVar, container);
        }

        private static IConfigVarContainer GetContainer(object obj)
        {
            return obj switch
            {
                SharedStatic<int> intField => new ConfigVarSharedStaticContainer<int>(intField),
                SharedStatic<float> floatField => new ConfigVarSharedStaticContainer<float>(floatField),
                SharedStatic<bool> boolField => new ConfigVarSharedStaticContainer<bool>(boolField),
                SharedStatic<FixedString32Bytes> stringField32 => new ConfigVarSharedStaticStringContainer<FixedString32Bytes>(stringField32),
                SharedStatic<FixedString64Bytes> stringField64 => new ConfigVarSharedStaticStringContainer<FixedString64Bytes>(stringField64),
                SharedStatic<FixedString128Bytes> stringField128 => new ConfigVarSharedStaticStringContainer<FixedString128Bytes>(stringField128),
                SharedStatic<FixedString512Bytes> stringField512 => new ConfigVarSharedStaticStringContainer<FixedString512Bytes>(stringField512),
                SharedStatic<FixedString4096Bytes> stringField4096 => new ConfigVarSharedStaticStringContainer<FixedString4096Bytes>(stringField4096),
                _ => new NullConfigVarContainer(),
            };
        }
    }
}
