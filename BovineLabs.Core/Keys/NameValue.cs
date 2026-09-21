namespace BovineLabs.Core.Keys
{
    using System;
    using UnityEngine;

    [Serializable]
    public struct NameValue<T>
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private T _value;

        public NameValue(string name, T value)
        {
            _name = name;
            _value = value;
        }

        public string Name => _name;

        public T Value => _value;
    }
}
