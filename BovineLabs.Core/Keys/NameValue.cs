// <copyright file="NameValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct NameValue
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private byte value;

        /// <summary> Initializes a new instance of the <see cref="NameValue"/> struct. </summary>
        /// <param name="name"> They name. </param>
        /// <param name="value"> The value. </param>
        public NameValue(string name, byte value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name
        {
            get => this.name;
            internal set => this.name = value;
        }

        public byte Value
        {
            get => this.value;
            internal set => this.value = value;
        }
    }
}
