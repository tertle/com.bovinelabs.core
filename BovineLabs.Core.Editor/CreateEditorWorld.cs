// <copyright file="CreateEditorWorld.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CREATE_EDITOR_WORLD
namespace BovineLabs.Core.Editor
{
    using System.Threading.Tasks;
    using Unity.Entities;
    using UnityEditor;

    internal static class CreateEditorWorld
    {
        internal static async Task Initialize()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredEditMode)
                {
                    DefaultWorldInitialization.DefaultLazyEditModeInitialize();
                }
            };

            await Task.Yield();

            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
        }
    }
}
#endif
