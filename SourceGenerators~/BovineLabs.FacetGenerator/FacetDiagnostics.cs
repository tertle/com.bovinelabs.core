// <copyright file="FacetDiagnostics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.FacetGenerator
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
    internal static class FacetDiagnostics
    {
        private const string Category = "BovineLabs.FacetGenerator";

        internal static readonly DiagnosticDescriptor MissingPartialDescriptor = new DiagnosticDescriptor(
            "BLFCT0001",
            "Facet must be partial",
            "IFacet '{0}' must be declared partial to generate helpers",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedFieldDescriptor = new DiagnosticDescriptor(
            "BLFCT0002",
            "Facet field type not supported",
            "Field '{0}' of type '{1}' is not supported. Supported types are RefRO<T>, RefRW<T>, EnabledRefRO<T>, EnabledRefRW<T>, DynamicBuffer<T>, Entity, and fields marked with [Singleton].",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor NoFieldsDescriptor = new DiagnosticDescriptor(
            "BLFCT0003",
            "Facet has no supported fields",
            "IFacet '{0}' does not contain any supported fields",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor ReadOnlyRefRWDescriptor = new DiagnosticDescriptor(
            "BLFCT0004",
            "ReadOnly not supported on RefRW",
            "ReadOnlyAttribute is not supported on '{0}'. Use RefRO<{1}> if you only require read-only access.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor NonStructDescriptor = new DiagnosticDescriptor(
            "BLFCT0005",
            "IFacet must be a struct",
            "Type '{0}' implements IFacet but is not a struct. Only partial structs are supported.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor InvalidFacetFieldDescriptor = new DiagnosticDescriptor(
            "BLFCT0006",
            "Facet field must be another IFacet struct",
            "Field '{0}' is marked with [Facet] but '{1}' does not implement IFacet",
            Category,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic MissingPartial(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(MissingPartialDescriptor, location, typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedField(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedFieldDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                fieldSymbol.Type.ToDisplayString(FacetGenerator.ShortTypeFormat));
        }

        public static Diagnostic NoFields(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(NoFieldsDescriptor, location, typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat));
        }

        public static Diagnostic ReadOnlyRefRW(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                ReadOnlyRefRWDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                GetComponentName(fieldSymbol));
        }

        public static Diagnostic NonStructFacet(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                NonStructDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(FacetGenerator.ShortTypeFormat));
        }

        public static Diagnostic InvalidFacetField(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                InvalidFacetFieldDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                fieldSymbol.Type.ToDisplayString(FacetGenerator.ShortTypeFormat));
        }

        private static string GetComponentName(IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.Type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0].ToDisplayString(FacetGenerator.ShortTypeFormat);
            }

            return "T";
        }
    }
}
