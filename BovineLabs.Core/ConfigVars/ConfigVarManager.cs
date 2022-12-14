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

    /// <summary> The manager for the config vars. Is pretty automated. </summary>
    public static class ConfigVarManager
    {
        private static readonly Regex ValidateNameRe = new(@"^[a-z_+-][a-z0-9_+.-]*$");

        private static readonly Dictionary<ConfigVarAttribute, IConfigVarContainer> NameConfigVars = new();

        private static bool isInitialized;

        /// <summary> Gets a readonly collection of all the configuration variables in the assembly. </summary>
        public static IReadOnlyDictionary<ConfigVarAttribute, IConfigVarContainer> All => NameConfigVars;

        /// <summary> Initializes the <see cref="ConfigVarAttribute" />s throughout the project. </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Init()
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

                if (container == null)
                {
                    Debug.LogError($"ConfigVar on field ({field.Name} in {field.DeclaringType?.Name}) of type ({field.FieldType}) is not a supported type");
                    continue;
                }

                RegisterConfigVar(configVar, container);

                if (CommandLineArgs.TryGetArgument(configVar.Name, out var value))
                {
                    container.Value = value;
                }
                else
                {
                    // always load in editor
                    if (Application.isEditor || configVar.Flags.HasFlag(ConfigVarFlags.Save))
                    {
                        container.Value = PlayerPrefs.GetString(configVar.Name, configVar.DefaultValue);
                    }
                    else
                    {
                        container.Value = configVar.DefaultValue;
                    }
                }
            }
        }

        private static void Shutdown()
        {
            foreach (var (configVar, container) in NameConfigVars)
            {
                // always save in editor
                if (Application.isEditor || configVar.Flags.HasFlag(ConfigVarFlags.Save))
                {
                    PlayerPrefs.SetString(configVar.Name, container.Value);
                }
            }

            NameConfigVars.Clear();
            isInitialized = false;
        }

        private static IEnumerable<(ConfigVarAttribute ConfigVar, FieldInfo Field)> FindAllConfigVars()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    Debug.LogWarning($"Unable to load types for assembly {assembly.FullName}");
                    continue;
                }

                foreach (var type in types)
                {
                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    {
                        var configVar = field.GetCustomAttribute<ConfigVarAttribute>();

                        if (configVar == null)
                        {
                            continue;
                        }

                        if (!field.IsStatic)
                        {
                            Debug.LogError($"Cannot use ConfigVar attribute on non-static fields. Field ({field.Name}) ParentType ({type})");
                            continue;
                        }

                        yield return (configVar, field);
                    }
                }
            }
        }

        private static void RegisterConfigVar(ConfigVarAttribute configVar, IConfigVarContainer container)
        {
            if (NameConfigVars.ContainsKey(configVar))
            {
                Debug.LogError($"Trying to register ConfigVar {configVar.Name} twice");
                return;
            }

            if (!ValidateNameRe.IsMatch(configVar.Name))
            {
                Debug.LogError($"Trying to register ConfigVar with invalid name: {configVar.Name}");
                return;
            }

            NameConfigVars.Add(configVar, container);
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
                _ => null,
            };
        }
    }
}
