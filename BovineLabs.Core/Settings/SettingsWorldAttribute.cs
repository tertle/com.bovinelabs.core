namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SettingsWorldAttribute : Attribute
    {
        public SettingsWorldAttribute(params string[] worlds)
        {
            this.Worlds = worlds;
        }

        /// <summary>
        /// Case-insensitive match against EditorSettings.
        /// </summary>
        public string[] Worlds { get; }
    }
}
