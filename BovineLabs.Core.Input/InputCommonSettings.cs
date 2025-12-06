// <copyright file="InputCommonSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [SettingsGroup("Core")]
    public class InputCommonSettings : SettingsSingleton<InputCommonSettings>
    {
        private readonly Dictionary<Type, IInputSettings> allSettings = new();

        [SerializeField]
        private InputActionAsset asset;

        [SerializeField]
        private string[] defaultEnabled = Array.Empty<string>();

        [SerializeField]
        private InputActionReference cursorPosition;

        [SerializeField]
        [SerializeReference]
        private List<IInputSettings> settings = new();

#if UNITY_EDITOR || BL_DEBUG
        [SerializeField]
        [SerializeReference]
        private List<IInputSettings> debugSettings = new();
#endif

        public InputActionAsset Asset => this.asset;

        public InputActionReference CursorPosition => this.cursorPosition;

        public IReadOnlyList<string> DefaultEnabled => this.defaultEnabled;

        public IReadOnlyList<IInputSettings> Settings => this.settings;

#if UNITY_EDITOR || BL_DEBUG
        public IReadOnlyList<IInputSettings> DebugSettings => this.debugSettings;
#endif

        public bool TryGetSettings<T>(out T action)
            where T : class, IInputSettings
        {
            if (this.allSettings.TryGetValue(typeof(T), out var s))
            {
                action = s as T;
                return action != null;
            }

            action = null;
            return false;
        }

        protected override void OnInitialize()
        {
            this.allSettings.Clear();

            foreach (var setting in this.settings)
            {
                if (setting != null)
                {
                    this.allSettings.Add(setting.GetType(), setting);
                }
            }

#if UNITY_EDITOR || BL_DEBUG
            foreach (var setting in this.debugSettings)
            {
                if (setting != null)
                {
                    this.allSettings.Add(setting.GetType(), setting);
                }
            }
#endif
        }

        /// <inheritdoc />
//         public override void Bake(Baker<SettingsAuthoring> baker)
//         {
//             var entity = baker.GetEntity(TransformUsageFlags.None);
//
//             var cts = new ComponentTypeSet(typeof(InputDefault), typeof(InputDefaultEnabled), typeof(InputCommon), typeof(InputActionMapEnable));
//             baker.AddComponent(entity, cts);
//
//             baker.SetComponent(entity, new InputDefault
//             {
//                 Asset = this.asset,
//                 CursorPosition = this.cursorPosition,
//             });
//
//             var toEnable = baker.SetBuffer<InputDefaultEnabled>(entity);
//             foreach (var actionMap in this.defaultEnabled)
//             {
//                 toEnable.Add(new InputDefaultEnabled { ActionMap = actionMap });
//             }
//
//             var wrapper = new BakerWrapper(baker, entity);
//
//             foreach (var s in this.settings)
//             {
//                 s?.Bake(wrapper);
//             }
//
// #if !BL_DEBUG
//             if (baker.IsBakingForEditor())
// #endif
//             {
//                 foreach (var s in this.debugSettings)
//                 {
//                     s?.Bake(wrapper);
//                 }
//             }
//         }


    }
}
