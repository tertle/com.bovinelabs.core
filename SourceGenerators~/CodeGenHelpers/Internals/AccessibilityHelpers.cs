using Microsoft.CodeAnalysis;

#nullable enable
namespace CodeGenHelpers.Internals
{
    internal static class AccessibilityHelpers
    {
        public static string? Code(Accessibility accessModifier) =>
            accessModifier switch
            {
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.NotApplicable => null,
                _ => accessModifier.ToString().ToLowerInvariant()
            };

        public static string? Code(Accessibility? accessModifier) =>
            accessModifier.HasValue ? Code(accessModifier.Value) : null;

        public static string? Code(Accessibility? accessModifier, Accessibility defaultValue) =>
            accessModifier.HasValue ? Code(accessModifier.Value) : Code(defaultValue);
    }
}
