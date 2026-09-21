namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsGroupAttribute : Attribute
    {
        public SettingsGroupAttribute(string group)
        {
            Group = group;
        }

        public string Group { get; }
    }
}
