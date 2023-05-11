// <copyright file="NameValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
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
