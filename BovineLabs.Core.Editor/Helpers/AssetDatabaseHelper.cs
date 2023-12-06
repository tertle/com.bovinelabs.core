// <copyright file="AssetDatabaseHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Helpers
{
    using System.IO;
    using UnityEditor;

    public static class AssetDatabaseHelper
    {
        public static void CreateDirectories(ref string directory)
        {
            directory = directory.Replace('\\', '/');

            var combo = string.Empty;
            var dir = directory.Split('/');
            foreach (var d in dir)
            {
                if (string.IsNullOrWhiteSpace(d))
                {
                    continue;
                }

                var p = Path.Combine(combo, d);
                if (!AssetDatabase.IsValidFolder(p))
                {
                    AssetDatabase.CreateFolder(combo, d);
                }

                combo = p;
            }
        }
    }
}
