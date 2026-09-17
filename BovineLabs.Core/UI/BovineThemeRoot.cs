namespace BovineLabs.Core.UI
{
    using UnityEngine.UIElements;

    [UxmlElement]
    public partial class BovineThemeRoot : VisualElement
    {
        public BovineThemeRoot()
        {
            BovineThemeUtility.Apply(this);
        }
    }
}
