// <copyright file="InputCommonSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Authoring.Input
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Input;
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Object = UnityEngine.Object;

    [SettingsGroup("Core")]
    [SettingsWorld("Client")]
    public class InputCommonSettings : SettingsBase
    {
        [SerializeField]
        private InputActionAsset? asset;

        [SerializeField]
        private string[] defaultEnabled = Array.Empty<string>();

        [SerializeField]
        private InputActionReference? cursorPosition;

        [SerializeField]
        [SerializeReference]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Unity serialization")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local", Justification = "Unity serialization")]
        private List<IInputSettings> settings = new();

#if UNITY_EDITOR || BL_DEBUG
        [SerializeField]
        [SerializeReference]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Unity serialization")]
        [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local", Justification = "Unity serialization")]
        private List<IInputSettings> debugSettings = new();
#endif

        /// <inheritdoc />
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            var defaultSettings = new InputDefault
            {
                Asset = this.asset!,
                CursorPosition = baker.DependsOn(this.cursorPosition)!,
            };

            var toEnable = baker.AddBuffer<InputDefaultEnabled>(entity);
            foreach (var actionMap in this.defaultEnabled)
            {
                toEnable.Add(new InputDefaultEnabled { ActionMap = actionMap });
            }

            baker.AddComponent(entity, defaultSettings);
            baker.AddComponent<InputCommon>(entity);
            baker.AddComponent<InputActionMapEnable>(entity);

            var wrapper = new BakerWrapper(baker, entity);

            foreach (var s in this.settings)
            {
                s?.Bake(wrapper);
            }

#if !BL_DEBUG
            if (baker.IsBakingForEditor())
#endif
            {
                foreach (var s in this.debugSettings)
                {
                    s?.Bake(wrapper);
                }
            }
        }

        private class BakerWrapper : IBakerWrapper
        {
            private readonly IBaker baker;
            private readonly Entity entity;

            public BakerWrapper(IBaker baker, Entity entity)
            {
                this.baker = baker;
                this.entity = entity;
            }

            public void AddComponent<T>(T component)
                where T : unmanaged, IComponentData
            {
                this.baker.AddComponent(this.entity, component);
            }

            public T DependsOn<T>(T obj)
                where T : Object
            {
                return this.baker.DependsOn(obj);
            }
        }
    }
}
#endif
