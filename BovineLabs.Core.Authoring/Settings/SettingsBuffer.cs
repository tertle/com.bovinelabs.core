// <copyright file="SettingsBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    [Serializable]
    public abstract class SettingsBuffer<T> : SettingsBase
        where T : unmanaged, IBufferElementData
    {
        [SerializeField]
        private T[] buffer = Array.Empty<T>();

        /// <summary> Gets or sets the component. </summary>
        public T[] Buffer
        {
            get => this.buffer;
            protected internal set => this.buffer = value;
        }

        /// <inheritdoc />
        public sealed override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entityBuffer = baker.AddBuffer<T>(baker.GetEntity(TransformUsageFlags.None));
            foreach (var b in this.buffer)
            {
                entityBuffer.Add(b);
            }

            this.CustomBake(baker);
        }

        /// <summary> Implement to add extra custom conversion. </summary>
        /// <param name="baker"> The baker. </param>
        protected virtual void CustomBake(IBaker baker)
        {
        }

        /// <summary> Gets optional default value to reset to. </summary>
        /// <returns> The default values for the component. </returns>
        protected virtual T[] GetDefaults()
        {
            return Array.Empty<T>();
        }

        private void Reset()
        {
            this.buffer = this.GetDefaults();
        }
    }
}
