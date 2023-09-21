// <copyright file="ToolbarUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using System.Linq;
    using BovineLabs.Core.UI;
    using BovineLabs.Core.Utility;

    public static class ToolbarUtility
    {
        public static void UpdateWorldList(PopupField worldsField)
        {
            var worlds = WorldUtility.AllExcludingAdvanced().ToArray();

            worldsField.SetDisplayNames(worlds.Select(w => w.Name));

            // Build the selected world
            if (worlds.Length == 0)
            {
                worldsField.value = -1;
                return;
            }

            // By default we try select server world because it's most important
            var value = 0;

            for (var i = 0; i < worlds.Length; i++)
            {
                var world = worlds[i];
                if (world.Name == "Server World")
                {
                    value = i;
                    break;
                }
            }

            worldsField.value = value;
        }
    }
}
#endif
