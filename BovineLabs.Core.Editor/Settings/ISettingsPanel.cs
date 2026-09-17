namespace BovineLabs.Core.Editor.Settings
{
    using UnityEngine.UIElements;

    public interface ISettingsPanel
    {
        string DisplayName { get; }

        string GroupName { get; }

        bool IsEmpty { get; }

        void OnActivate(string searchContext, VisualElement rootElement);

        void OnDeactivate();

        bool MatchesFilter(string searchContext, bool allowEmpty);
    }
}
