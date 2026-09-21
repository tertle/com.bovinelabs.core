namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class SettingSubDirectoryAttribute : Attribute
    {
        public SettingSubDirectoryAttribute(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }
    }
}
