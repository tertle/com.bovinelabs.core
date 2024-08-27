// <copyright file="GameStates.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.Data
{
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.Settings;

    /// <summary> The client game states. </summary>
    /// <remarks> Note these values represent the index of the flag. </remarks>
    [SettingsGroup("Core")]
    public class GameStates : KSettings<GameStates>
    {
    }
}
