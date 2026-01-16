// <copyright file="InitializeAllOnLoad.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.ObjectManagement;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Editor.Utility;
    using BovineLabs.Core.Editor.Welcome;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.Utility;

    /// <summary>
    /// To avoid multiple Initialize on load calls, do it in a single place
    /// </summary>
#if !BL_CORE_EXTENSIONS
    [UnityEditor.InitializeOnLoad]
#endif
    public static class InitializeAllOnLoad
    {
#if !BL_CORE_EXTENSIONS
        static InitializeAllOnLoad()
        {
            Initialize();
        }
#endif

        internal static void Initialize()
        {
            ConfigVarManager.Initialize();
            GlobalRandom.Initialize();
            PooledNativeList.Initialize();
            TypeManagerEx.Initialize();
            SettingsSingleton.InitializeInEditor();

            ScriptingDefineSymbolsEditor.Initialize();
            _ = CreateEditorWorld.Initialize();
            CreateAssetCreatorDefault.Initialize();
            LoadPrefabsAsEntities.Initialize();
            WelcomeWindow.Initialize();
            InspectorSearch.Initialize();
        }
    }
}
