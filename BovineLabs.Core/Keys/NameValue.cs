// <copyright file="NameValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct NameValue : IKKeyValue
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private int value;

        public string Name
        {
            get => this.name;
            set => this.name = value;
        }

        public int Value
        {
            get => this.value;
            set => this.value = value;
        }
    }
}
