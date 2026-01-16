// <copyright file="InitializeAllOnLoadExt.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using UnityEditor;
#if !BL_DISABLE_OBJECT_DEFINITION
    using BovineLabs.Core.Authoring.ObjectManagement;
#endif
#if !BL_DISABLE_SUBSCENE
    using BovineLabs.Core.Editor.SubScenes;
#endif

    [InitializeOnLoad]
    public static class InitializeAllOnLoadExt
    {
        static InitializeAllOnLoadExt()
        {
            InitializeAllOnLoad.Initialize();

            SetWorldToEditorWindows.Initialize();
            WorldSafeShutdown.Initialize();

#if !BL_DISABLE_SUBSCENE
            StartupSceneSwap.Initialize();
#endif
#if !BL_DISABLE_OBJECT_DEFINITION
            ObjectInstantiate.Initialize();
#endif
        }
    }
}
