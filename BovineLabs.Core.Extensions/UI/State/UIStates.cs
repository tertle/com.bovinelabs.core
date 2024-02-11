// <copyright file="UIStates.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    [SettingsGroup("UI")]
    public class UIStates : UIStatesBase
    {
        /// <inheritdoc/>
        protected override void Init()
        {
            K<UIStates>.Initialize(this.Keys);
        }
    }
}
#endif
