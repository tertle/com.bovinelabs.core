// <copyright file="ISettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Settings
{
    using BovineLabs.Basics.Extensions;
    using Unity.Entities;

    /// <summary> Internal interface for handling settings. </summary>
    public interface ISettings
    {
        /// <summary> Convert the settings file to entity representation. </summary>
        /// <param name="dstManager"> The worlds manager the entity belongs to. </param>
        /// <param name="entity"> The settings entity to store the settings on. </param>
        void Convert(EntityManager dstManager, Entity entity);
    }

    /// <summary> Extensions for <see cref="ISettings" />. </summary>
    public static class SettingsExtensions
    {
        /// <summary> Gets the display name of the settings. </summary>
        /// <param name="settings"> The settings. </param>
        /// <returns> The display name. </returns>
        public static string DisplayName(this ISettings settings)
        {
            var name = settings.GetType().Name;
            return name.TrimEnd("Settings").ToSentence();
        }
    }
}