namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class SettingSubDirectoryAttribute : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="SettingSubDirectoryAttribute" /> class. </summary>
        /// <param name="directory"> The subdirectory. </param>
        public SettingSubDirectoryAttribute(string directory)
        {
            this.Directory = directory;
        }

        public string Directory { get; }
    }
}
