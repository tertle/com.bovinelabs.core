// <copyright file="SourceGenHelpers.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.InputGenerator
{
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.CodeAnalysis;

    public static class SourceGenHelpers
    {
        public static bool ShouldRun(Compilation compilation, CancellationToken cancellationToken)
        {
            // Throw if we are cancelled
            cancellationToken.ThrowIfCancellationRequested();

            return compilation.ReferencedAssemblyNames.Any(n => n.Name == "BovineLabs.Core.Extensions") &&
                   compilation.ReferencedAssemblyNames.Any(n => n.Name == "Unity.Entities") &&
                   compilation.ReferencedAssemblyNames.Any(n => n.Name == "Unity.Collections") &&
                   compilation.ReferencedAssemblyNames.Any(n => n.Name == "Unity.InputSystem");
        }

        public static void Log(string message)
        {
            // Ignore IO exceptions in case there is already a lock, could use a named mutex but don't want to eat the performance cost
            try
            {
                string generatedCodePath = @"Logs\";
                var sourceGenLogPath = Path.Combine(generatedCodePath, "InputGenerator.log");
                using var writer = File.AppendText(sourceGenLogPath);
                writer.WriteLine(message);
            }
            catch (IOException)
            {
            }
        }
    }
}
