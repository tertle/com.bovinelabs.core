namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using UnityEngine;

    public interface IKsettings
    {
    }

    /// <summary>
    /// Implement KSettings&lt;T, TV&gt; or KSettingsBase&lt;T, TV&gt; rather than this base directly.
    /// </summary>
    [Serializable]
    [SettingSubDirectory("K")]
    public abstract class KSettingsBase<TV> : SettingsSingleton, IKsettings
    {
        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string _description = string.Empty;

        public abstract IEnumerable<NameValue<TV>> Keys { get; }
    }
}
