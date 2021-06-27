// <copyright file="Settings2.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Base class that automatically sets up a single component as a setting with a managed asset file. </summary>
    /// <typeparam name="TC"> The component setting. </typeparam>
    /// <typeparam name="TM"> The managed assets. </typeparam>
    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class Settings<TC, TM> : Settings
        where TC : struct, IComponentData
        where TM : IAssetSettings<TC>
    {
        [SerializeField]
        private TC component;

        [SerializeField]
        private TM assets;

        /// <summary> Gets or sets the component. </summary>
        public TC Component
        {
            get => this.component;
            protected internal set => this.component = value;
        }

        /// <summary> Gets or sets the component. </summary>
        public TM Assets
        {
            get => this.assets;
            protected internal set => this.assets = value;
        }

        /// <inheritdoc />
        public sealed override void Convert(EntityManager dstManager, Entity entity)
        {
            var c = this.assets.ApplyTo(this.component);
            dstManager.AddComponentData(entity, c);
            this.CustomConvert(dstManager, entity);
        }

        /// <summary> Implement to add extra custom conversion. </summary>
        /// <param name="dstManager"> The manager of the world. </param>
        /// <param name="entity"> The entity where the settings should be added. </param>
        protected virtual void CustomConvert(EntityManager dstManager, Entity entity)
        {
        }

        /// <summary> Gets optional default values to reset to. </summary>
        /// <returns> The default values for the component and asset fields. </returns>
        protected virtual (TC Component, TM Assets) GetDefaults()
        {
            return default;
        }

        private void Reset()
        {
            var (componentData, managedData) = this.GetDefaults();
            this.component = componentData;
            this.assets = managedData;
        }
    }
}