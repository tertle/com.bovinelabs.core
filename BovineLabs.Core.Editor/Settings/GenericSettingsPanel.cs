namespace BovineLabs.Core.Editor.Settings
{
    using BovineLabs.Core.Settings;
    using UnityEngine;

    internal sealed class GenericSettingsPanel<T> : SettingsBasePanel<T>
        where T : ScriptableObject, ISettings
    {
    }
}
