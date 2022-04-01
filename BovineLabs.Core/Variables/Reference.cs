// <copyright file="Reference.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Variables
{
    using System;
    using BovineLabs.Core.Variables;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary> A reference field that allows you to specify either a constant value or to share a variable between data. </summary>
    /// <typeparam name="TV"> The reference type. </typeparam>
    /// <typeparam name="T"> The type. </typeparam>
    [InlineProperty]
    public abstract class Reference<TV, T>
        where TV : Variable<T>
    {
        [SerializeField]
        [HideInInspector]
        private bool useVariable;

        [SerializeField]
        [HideIf("useVariable")]
        [HideLabel]
        [InlineButton("Toggle", "V")]
        private T constantValue;

        [SerializeField]
        [ShowIf("useVariable")]
        [HideLabel]
        [InlineButton("Toggle", "C")]
        private TV variable;

        /// <summary> Initializes a new instance of the <see cref="Reference{TR, T}"/> class. </summary>
        /// <param name="value"> A default constant. </param>
        public Reference(T value = default)
        {
            this.useVariable = false;
            this.constantValue = value;
        }

        /// <summary> Gets the value of this reference. </summary>
        public T Value => this.useVariable ? this.variable.Value : this.constantValue;

        public static implicit operator T(Reference<TV, T> reference)
        {
            return reference.Value;
        }

        private void Toggle()
        {
            this.useVariable = !this.useVariable;
        }
    }
}
