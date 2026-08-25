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
            "Field '{0}' of type '{1}' is not supported. Supported types are RefRO<T>, RefRW<T>, EnabledRefRO<T>, " +
            "FacetEnabledRefRW<T>, DynamicBuffer<T>, ComponentLookup<T>, BufferLookup<T>, Entity, EntityStorageInfo, " +
            "EntityStorageInfoLookup, and fields marked with [Singleton].",
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
            "ReadOnly not supported on writable facet reference",
            "ReadOnlyAttribute is not supported on '{0}'. Use {1}<{2}> if you only require read-only access.",
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

        internal static readonly DiagnosticDescriptor ReadOnlySingletonBufferDescriptor = new DiagnosticDescriptor(
            "BLFCT0007",
            "DynamicBuffer singleton must be ReadOnly",
            "Singleton DynamicBuffer field '{0}' must be marked with [ReadOnly]",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor FacetCycleDescriptor = new DiagnosticDescriptor(
            "BLFCT0008",
            "Facet reference cycle detected",
            "Field '{0}' creates a cyclic facet reference to '{1}'",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor SingletonAttributeConflictDescriptor = new DiagnosticDescriptor(
            "BLFCT0009",
            "Singleton attribute conflicts",
            "Field '{0}' cannot be marked with [Singleton] and [Facet] or [FacetOptional]",
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

        public static Diagnostic ReadOnlyRefRW(IFieldSymbol fieldSymbol, Location location, string replacementType)
        {
            return Diagnostic.Create(
                ReadOnlyRefRWDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                replacementType,
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

        public static Diagnostic ReadOnlySingletonBuffer(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                ReadOnlySingletonBufferDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name);
        }

        public static Diagnostic SingletonAttributeConflict(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                SingletonAttributeConflictDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name);
        }

        public static Diagnostic FacetCycle(IFieldSymbol fieldSymbol, Location location, INamedTypeSymbol facetType)
        {
            return Diagnostic.Create(
                FacetCycleDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                facetType.ToDisplayString(FacetGenerator.ShortTypeFormat));
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
