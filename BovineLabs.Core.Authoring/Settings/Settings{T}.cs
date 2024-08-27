// <copyright file="Settings{T}.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using JetBrains.Annotations;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Base class that automatically sets up a single component as a setting. </summary>
    /// <typeparam name="T"> The component setting. </typeparam>
    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class Settings<T> : SettingsBase
        where T : unmanaged, IComponentData
    {
        [SerializeField]
        private T component;

        /// <summary> Gets or sets the component. </summary>
        public T Component
        {
            get => this.component;
            protected internal set => this.component = value;
        }

#if UNITY_EDITOR
        [UsedImplicitly(ImplicitUseKindFlags.Access)]
        private string ComponentLabel => typeof(T).Name;
#endif

        /// <inheritdoc />
        public sealed override void Bake(Baker<SettingsAuthoring> baker)
        {
            baker.AddComponent(baker.GetEntity(TransformUsageFlags.None), this.component);

            this.CustomBake(baker);
        }

        /// <summary> Implement to add extra custom conversion. </summary>
        /// <param name="baker"> The baker. </param>
        protected virtual void CustomBake(IBaker baker)
        {
        }

        /// <summary> Gets optional default value to reset to. </summary>
        /// <returns> The default values for the component. </returns>
        protected virtual T GetDefaults()
        {
            return default;
        }

        private void Reset()
        {
            this.component = this.GetDefaults();
        }
    }
}
