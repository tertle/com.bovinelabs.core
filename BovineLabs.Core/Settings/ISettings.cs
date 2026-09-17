namespace BovineLabs.Core.Settings
{
    using BovineLabs.Core.Extensions;

    public interface ISettings
    {
    }

    public static class SettingsExtensions
    {
        public static string DisplayName(this ISettings settings)
        {
            var name = settings.GetType().Name;
            return name.TrimEnd("Settings").ToSentence();
        }
    }
}
