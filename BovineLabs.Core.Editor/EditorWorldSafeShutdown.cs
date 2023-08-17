// <copyright file="EditorWorldSafeShutdown.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.Internal;
    using UnityEditor;

    // TODO remove when fixed
    /// <summary> Workaround to fix entities 1.X errors when changing play mode states and have an entity selected and it errors. </summary>
    [InitializeOnLoad]
    public static class EditorWorldSafeShutdown
    {
        static EditorWorldSafeShutdown()
        {
            EditorApplication.playModeStateChanged += _ => EntitySelection.UnSelect();
        }
    }
}
