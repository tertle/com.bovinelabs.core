namespace BovineLabs.Core.UI
{
    using System;
    using Unity.Scripting.LifecycleManagement;
    using UnityEngine.UIElements;

    public static class BovineThemeUtility
    {
        public const string RootClass = "bl-theme";
        public const string WindowClass = "bl-theme-window";

        private const string WorksClass = "bl-theme--bovine-works";

        [NoAutoStaticsCleanup]
        private static StyleSheet styleSheet;

        public static void Apply(VisualElement root)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (styleSheet == null)
            {
                styleSheet = CoreSettings.I.ThemeStyleSheet;
                if (styleSheet == null)
                {
                    throw new InvalidOperationException("The required theme stylesheet is missing from CoreSettings.");
                }
            }

            if (!root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            root.AddToClassList(RootClass);
            root.AddToClassList(WorksClass);
        }
    }
}
