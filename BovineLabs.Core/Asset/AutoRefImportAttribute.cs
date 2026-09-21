namespace BovineLabs.Core.Asset
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AutoRefImportAttribute : Attribute
    {
        public AutoRefImportAttribute(string extension)
        {
            Extension = extension;
        }

        public string Extension { get; }
    }
}
