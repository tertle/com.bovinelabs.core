// <copyright file="Settings{T}.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using JetBrains.Annotations;
    using Sirenix.OdinInspector;
    using Unity.Entities;
    using UnityEngine;

    /// <summary> Base class that automatically sets up a single component as a setting. </summary>
    /// <typeparam name="T"> The component setting. </typeparam>
    [Serializable]
    [SuppressMessage("ReSharper", "Unity.RedundantSerializeFieldAttribute", Justification = "Required.")]
    public abstract class Settings<T> : Settings
        where T : struct, IComponentData
    {
        [SerializeField]
        [InlineProperty]
        [LabelText("$ComponentLabel")]
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
        public sealed override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, this.component);

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