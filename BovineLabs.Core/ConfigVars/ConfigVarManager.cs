// <copyright file="ConfigVarManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scripting.LifecycleManagement;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary> The manager for the config vars. Is pretty automated. </summary>
    public static partial class ConfigVarManager
    {
        private static readonly Regex ValidateNameRegex = new("^[a-z_+-][a-z0-9_+.-]*$");

        [NoAutoStaticsCleanup]
        private static readonly Dictionary<ConfigVarAttribute, IConfigVarContainer> AllInternal = new();
#if UNITY_EDITOR
        private const string EditorPrefsPrefix = "BovineLabs.Core.ConfigVars";
#endif

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
        [OnCodeLoaded]
        private static void Initialize()
        {
            TypeManager.Initialize();

            // We make sure to always initialize the logger first so we can use this here
            foreach (var (configVar, field) in FindAllConfigVars().OrderByDescending(v => v.ConfigVar.Name == BLLogger.LogLevelName))
            {
                var fieldValue = field.GetValue(null);

                var container = GetContainer(fieldValue);

                if (container is NullConfigVarContainer)
                {
                    BLGlobalLogger.LogErrorString($"ConfigVar on field ({field.Name} in {field.DeclaringType?.Name}) of type ({field.FieldType}) is not a supported type");
                    continue;
                }

                RegisterConfigVar(configVar, container);

                if (CommandLineArgs.TryGetArgument($"-{configVar.Name}", out var value))
                {
                    try
                    {
                        container.StringValue = value;
                        BLGlobalLogger.LogInfoString($"{configVar.Name} set from command line to {value}");
                    }
                    catch (Exception)
                    {
                        BLGlobalLogger.LogErrorString(
                            $"Trying to set a ConfigVar {configVar.Name} value of {value} which is not in the right type format. Falling back to default.");

                        container.StringValue = configVar.DefaultValue;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    container.StringValue = EditorPrefs.GetString(GetEditorPrefsKey(configVar.Name), configVar.DefaultValue);
                    BLGlobalLogger.LogDebugString($"{configVar.Name} set from editor prefs to {container.StringValue}");
#else
                    container.StringValue = configVar.DefaultValue;
                    BLGlobalLogger.LogDebugString($"{configVar.Name} set from default value to {container.StringValue}");
#endif

                }
            }
        }

        [OnCodeUnloading]
        private static void Shutdown()
        {
#if UNITY_EDITOR
            foreach (var (configVar, container) in AllInternal)
            {
                EditorPrefs.SetString(GetEditorPrefsKey(configVar.Name), container.StringValue);
            }
#endif

            AllInternal.Clear();
        }

#if UNITY_EDITOR
        internal static string GetEditorPrefsKey(string configVarName)
        {
            return $"{EditorPrefsPrefix}.{PlayerSettings.productGUID}.{configVarName}";
        }
#endif

        private static void RegisterConfigVar(ConfigVarAttribute configVar, IConfigVarContainer container)
        {
            if (AllInternal.ContainsKey(configVar))
            {
                BLGlobalLogger.LogErrorString($"Trying to register ConfigVar {configVar.Name} twice");
                return;
            }

            if (!ValidateNameRegex.IsMatch(configVar.Name))
            {
                BLGlobalLogger.LogErrorString($"Trying to register ConfigVar with invalid name: {configVar.Name}");
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
                SharedStatic<Color> colorField => new ConfigVarSharedStaticColorContainer(colorField),
                SharedStatic<Vector4> colorField => new ConfigVarSharedStaticVector4Container(colorField),
                SharedStatic<Rect> colorField => new ConfigVarSharedStaticRectContainer(colorField),
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
