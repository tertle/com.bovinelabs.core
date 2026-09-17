namespace BovineLabs.Core.Keys
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct NameValue<T>
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private T value;

        public NameValue(string name, T value)
        {
            this.name = name;
            this.value = value;
        }

        public string Name => this.name;

        public T Value => this.value;
    }
}
