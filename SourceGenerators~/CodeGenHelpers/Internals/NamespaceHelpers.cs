#nullable enable
namespace CodeGenHelpers.Internals
{
    internal static class NamespaceHelpers
    {
        public static bool IsGlobalNamespace(string? @namespace)
        {
            return string.IsNullOrWhiteSpace(@namespace) ||
                @namespace!.Contains("<global namespace>") ||
                @namespace == "<global namespace>" ||
                @namespace == "global" ||
                (@namespace.StartsWith("<") && @namespace.EndsWith(">"));
        }
    }
}