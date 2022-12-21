// <copyright file="EditorWorldSafeCleanup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorToolbar
{
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.LowLevel;

    [InitializeOnLoad]
    public static class EditorWorldSafeCleanup
    {
        static EditorWorldSafeCleanup()
        {
            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            foreach (var w in World.s_AllWorlds)
            {
                ScriptBehaviourUpdateOrder.RemoveWorldFromPlayerLoop(w, ref playerLoop);
            }

            PlayerLoop.SetPlayerLoop(playerLoop);
            World.DisposeAllWorlds();
        }
    }
}
