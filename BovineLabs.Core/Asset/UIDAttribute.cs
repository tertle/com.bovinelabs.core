namespace BovineLabs.Core.Asset
{
    using System;
    using UnityEngine;

    public class UIDAttribute : PropertyAttribute
    {
        public UIDAttribute(string type)
        {
            Type = type;
        }

        public UIDAttribute(Type type)
        {
            Type = type.Name;
        }

        public string Type { get; }
    }
}
