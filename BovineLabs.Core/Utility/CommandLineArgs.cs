namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using Unity.Scripting.LifecycleManagement;

    public static class CommandLineArgs
    {
        [NoAutoStaticsCleanup]
        private static readonly List<string> Args = new(Environment.GetCommandLineArgs());

        public static bool TryGetArgument(string arg, out string value)
        {
            var idx = Args.IndexOf(arg);
            if (idx < 0)
            {
                value = string.Empty;
                return false;
            }

            value = idx < Args.Count - 1 ? Args[idx + 1] : string.Empty;

            return true;
        }

        public static bool Contains(string arg)
        {
            return Args.Contains(arg);
        }
    }
}
