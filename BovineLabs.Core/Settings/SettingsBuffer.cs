// <copyright file="SettingsBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    [Serializable]
    public abstract class SettingsBuffer<T> : Settings
        where T : struct, IBufferElementData
    {
        [SerializeField]
        private T[] buffer;

        /// <summary> Gets or sets the component. </summary>
        public T[] Buffer
        {
            get => this.buffer;
            protected internal set => this.buffer = value;
        }

        /// <inheritdoc />
        public sealed override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject owner)
        {
            var entityBuffer = dstManager.AddBuffer<T>(entity);
            foreach (var b in this.buffer)
            {
                entityBuffer.Add(b);
            }

            this.CustomConvert(dstManager, entity, conversionSystem);
        }

        /// <summary> Implement to add extra custom conversion. </summary>
        /// <param name="dstManager"> The manager of the world. </param>
        /// <param name="entity"> The entity where the settings should be added. </param>
        /// <param name="conversionSystem"> The conversion system. </param>
        protected virtual void CustomConvert(EntityManager dstManager, Entity entity, GameObjectConversionSystem conversionSystem)
        {
        }

        /// <summary> Gets optional default value to reset to. </summary>
        /// <returns> The default values for the component. </returns>
        protected virtual T[] GetDefaults()
        {
            return default;
        }

        private void Reset()
        {
            this.buffer = this.GetDefaults();
        }
    }
}
