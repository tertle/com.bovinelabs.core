namespace CodeGenHelpers
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    public static class SourceGenHelpers
    {
        /// <summary>
        /// Taken from Netcode source generators.
        /// </summary>
        public static bool IsBuildTime()
        {
            // We want to be exclusive rather than inclusive here to avoid any issues with unknown processes, Unity changes, and testing
            Assembly assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                return false;
            }
            // VS Code
            if (assembly.FullName.StartsWith("Microsoft",
                StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
            // Visual Studio
            if (assembly.FullName.StartsWith("ServiceHub",
                StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }
            // Rider
            if (assembly.FullName.StartsWith("JetBrains",
                StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static void Log(string message)
        {
            // Logging should never throw; this is best-effort and used primarily from exception handlers.
            try
            {
                var generatedCodePath = "Logs";
                Directory.CreateDirectory(generatedCodePath);

                var sourceGenLogPath = Path.Combine(generatedCodePath, "CoreGenerator.log");
                using var writer = File.AppendText(sourceGenLogPath);

                var timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                var prefix = $"[{timestamp}] ";

                if (message == null)
                {
                    writer.WriteLine(prefix);
                    return;
                }

                var normalized = message.Replace("\r\n", "\n");
                var lines = normalized.Split('\n');
                foreach (var line in lines)
                {
                    writer.WriteLine(prefix + line);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
