// <copyright file="CreateEditorWorld.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CREATE_EDITOR_WORLD
namespace BovineLabs.Core.Editor
{
    using System.Threading.Tasks;
    using Unity.Entities;
    using UnityEditor;

    [InitializeOnLoad]
    public static class CreateEditorWorld
    {
        static CreateEditorWorld()
        {
            _ = Initialize();
        }

        private static async Task Initialize()
        {
            await Task.Yield();

            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
        }
    }
}
#endif
