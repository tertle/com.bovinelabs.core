// <copyright file="DynamicDiagnostics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.DynamicGenerator
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
    internal static class DynamicDiagnostics
    {
        private const string Category = "BovineLabs.DynamicGenerator";

        internal static readonly DiagnosticDescriptor NonStructDescriptor = new DiagnosticDescriptor(
            "BLDYN0001",
            "Dynamic buffer must be a struct",
            "Type '{0}' implements an IDynamic* interface but is not a struct. Only structs are supported.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor MultipleInterfacesDescriptor = new DiagnosticDescriptor(
            "BLDYN0002",
            "Multiple dynamic interfaces detected",
            "Type '{0}' implements multiple IDynamic* interfaces. Implement only one per type.",
            Category,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic NonStruct(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                NonStructDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic MultipleInterfaces(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                MultipleInterfacesDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }
    }
}
