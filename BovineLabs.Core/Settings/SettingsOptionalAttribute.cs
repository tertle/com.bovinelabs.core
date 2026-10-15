namespace BovineLabs.Core.Settings
{
    using System;

    /// <summary>Settings require explicit creation; package registration, Editor world preparation and window discovery leave them absent.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SettingsOptionalAttribute : Attribute
    {
    }
}
