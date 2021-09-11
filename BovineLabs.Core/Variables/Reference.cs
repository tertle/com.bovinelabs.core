// <copyright file="Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using BovineLabs.Core.Variables;
    using UnityEngine;

    /// <summary> A reference field that allows you to specify either a constant value or to share a variable between data. </summary>
    /// <typeparam name="TR"> The reference type. </typeparam>
    /// <typeparam name="T"> The type. </typeparam>
    public abstract class Reference<TR, T>
        where TR : Variable<T>
    {
        [SerializeField]
        private bool useVariable;

        [SerializeField]
        private T constantValue;

        [SerializeField]
        private TR variable;

        /// <summary>
        /// Initializes a new instance of the <see cref="Reference{TR, T}"/> class.
        /// </summary>
        /// <param name="value"> A default constant. </param>
        public Reference(T value = default)
        {
            this.useVariable = false;
            this.constantValue = value;
        }

        /// <summary> Gets the value of this reference. </summary>
        public T Value => this.useVariable ? this.variable.Value : this.constantValue;

        public static implicit operator T(Reference<TR, T> reference)
        {
            return reference.Value;
        }
    }
}
