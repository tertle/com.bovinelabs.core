namespace BovineLabs.Savanna
{
    using System;

    [Flags]
    public enum SaveFeature : byte
    {
        None = 0,

        /// <summary>
        /// Allows runtime addition during load, requiring slower deferred ECB processing.
        /// </summary>
        AddComponent = 1,
    }
}
