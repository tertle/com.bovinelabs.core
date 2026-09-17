namespace BovineLabs.Core.Settings
{
    using System;

    /// <summary>
    /// Attribute that causes settings to show in the window even if they're empty.
    /// Use this for when you have a custom editor showing properties that are hidden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AlwaysShowSettingsAttribute : Attribute
    {
    }
}
