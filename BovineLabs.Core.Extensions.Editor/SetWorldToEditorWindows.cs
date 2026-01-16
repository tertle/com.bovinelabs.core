// <copyright file="SetWorldToEditorWindows.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using System.Reflection;
    using Unity.Entities;
    using Unity.Entities.Editor;
    using UnityEditor;
    using Resources = UnityEngine.Resources;

    internal static class SetWorldToEditorWindows
    {
        internal static void Initialize()
        {
            BovineLabsBootstrap.GameWorldCreated += SetWorldToDOTSEditorWindow;
        }

        private static void SetWorldToDOTSEditorWindow(World world)
        {
            if (world != World.DefaultGameObjectInjectionWorld)
            {
                return;
            }

            // Insert at index 0 for future windows you open will default to this
            World.s_AllWorlds.Remove(world);
            World.s_AllWorlds.Insert(0, world);

            var worldSelector = typeof(DOTSEditorWindow).GetField("m_WorldSelector", BindingFlags.Instance | BindingFlags.NonPublic)!;

            // Set all open window worlds
            foreach (var o in Resources.FindObjectsOfTypeAll<DOTSEditorWindow>())
            {
                // If the window is open but is a hidden tab, it won't have been setup yet
                if (worldSelector.GetValue(o) == null)
                {
                    continue;
                }

                o.SelectedWorld = world;
            }
        }
    }
}
