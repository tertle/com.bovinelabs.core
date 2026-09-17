namespace BovineLabs.Core.Keys
{
    using System;
    using BovineLabs.Core.Inspectors;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Field)]
    public class KAttribute : PropertyAttribute, IBitFieldAttribute
    {
        public KAttribute(string settings, bool flags = false)
        {
            this.Settings = settings;
            this.Flags = flags;
        }

        public string Settings { get; }

        public bool Flags { get; }
    }
}
