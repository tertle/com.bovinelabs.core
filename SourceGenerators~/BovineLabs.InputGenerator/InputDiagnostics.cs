// <copyright file="InputDiagnostics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.InputGenerator
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
    internal static class InputDiagnostics
    {
        private const string Category = "BovineLabs.InputGenerator";

        internal static readonly DiagnosticDescriptor MissingPartialDescriptor = new DiagnosticDescriptor(
            "BLIAG0001",
            "Input component must be partial",
            "Type '{0}' must be declared partial to generate input helpers",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedFieldDescriptor = new DiagnosticDescriptor(
            "BLIAG0002",
            "Unsupported input field type",
            "Field '{0}' of type '{1}' is not supported. Supported types are bool, float, float2, half, ButtonState, and InputEvent.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor InvalidUpDownDescriptor = new DiagnosticDescriptor(
            "BLIAG0003",
            "Invalid InputActionDown/InputActionUp usage",
            "Field '{0}' must be of type bool or InputEvent when using InputActionDown or InputActionUp",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor InvalidDeltaDescriptor = new DiagnosticDescriptor(
            "BLIAG0004",
            "Invalid InputActionDelta usage",
            "Field '{0}' must be a numeric type (float, float2, or half) when using InputActionDelta",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor NoSupportedFieldsDescriptor = new DiagnosticDescriptor(
            "BLIAG0005",
            "No supported input actions found",
            "Type '{0}' uses input action attributes but none of the fields are supported",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor NonStructDescriptor = new DiagnosticDescriptor(
            "BLIAG0006",
            "Input component must be a struct",
            "Type '{0}' implements IComponentData but is not a struct. Only structs are supported.",
            Category,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic MissingPartial(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(MissingPartialDescriptor, location, typeSymbol.ToDisplayString(InputActionGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedField(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedFieldDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name,
                fieldSymbol.Type.ToDisplayString(InputActionGenerator.ShortTypeFormat));
        }

        public static Diagnostic InvalidUpDown(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                InvalidUpDownDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name);
        }

        public static Diagnostic InvalidDelta(IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                InvalidDeltaDescriptor,
                location ?? fieldSymbol.Locations[0],
                fieldSymbol.Name);
        }

        public static Diagnostic NoSupportedFields(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                NoSupportedFieldsDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(InputActionGenerator.ShortTypeFormat));
        }

        public static Diagnostic NonStruct(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                NonStructDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(InputActionGenerator.ShortTypeFormat));
        }
    }
}
