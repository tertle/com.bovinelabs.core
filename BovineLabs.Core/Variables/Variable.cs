// <copyright file="Variable.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary> The shared variable that <see cref="Reference{TR,T}"/> can use. </summary>
    /// <typeparam name="T"> The type. </typeparam>
    public abstract class Variable<T> : ScriptableObject
    {
#if UNITY_EDITOR
        [SerializeField]
        [Multiline]
        [UsedImplicitly]
        private string description;
#endif

        [SerializeField]
        private T value;

        /// <summary> Gets the value. </summary>
        public T Value => this.value;
    }
}