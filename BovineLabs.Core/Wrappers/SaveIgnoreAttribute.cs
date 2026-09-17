namespace BovineLabs.Savanna
{
    using System;

    /// <summary>
    /// Skips the field only on load; bytes remain in saves, so removing this attribute restores them. Group ignored fields together to reduce copy
    /// operations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveIgnoreAttribute : Attribute
    {
    }
}
