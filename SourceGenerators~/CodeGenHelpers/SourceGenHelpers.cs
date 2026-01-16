// <copyright file="SourceGenHelpers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace CodeGenHelpers
{
    using System;
    using System.Globalization;
    using System.IO;

    public static class SourceGenHelpers
    {
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
