// <copyright file="ISettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using BovineLabs.Core.Extensions;

    /// <summary> Internal interface for handling settings. </summary>
    public interface ISettings
    {
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
