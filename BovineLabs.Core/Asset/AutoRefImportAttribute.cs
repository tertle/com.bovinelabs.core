namespace BovineLabs.Core.Asset
{
    using System;

    /// <summary> Registers a non-.asset extension that should be scanned for AutoRef and IUID updates after import. </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AutoRefImportAttribute : Attribute
    {
        public AutoRefImportAttribute(string extension)
        {
            this.Extension = extension;
        }

        public string Extension { get; }
    }
}
