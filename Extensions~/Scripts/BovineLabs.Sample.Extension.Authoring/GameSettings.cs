// <copyright file="GameSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Settings;
    using BovineLabs.Sample.Extension.Data;
    using Unity.Entities;

    [SettingsGroup("Core")]
    [SettingsWorld("Shared")]
    public class GameSettings : BovineLabs.Core.Authoring.GameSettings
    {
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            base.Bake(baker);

            var entity = baker.GetEntity(TransformUsageFlags.None);
            var gameState = new GameState { Value = new BitArray256 { [0] = true } };

            // States
            baker.AddComponent(entity, gameState);
            baker.AddComponent<GameStatePrevious>(entity);
        }
    }
}
