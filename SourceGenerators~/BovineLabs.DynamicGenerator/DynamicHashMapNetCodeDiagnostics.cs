// <copyright file="DynamicHashMapNetCodeDiagnostics.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.DynamicGenerator
{
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    [SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
    internal static class DynamicHashMapNetCodeDiagnostics
    {
        private const string Category = "BovineLabs.DynamicGenerator.NetCode";

        internal static readonly DiagnosticDescriptor UnsupportedMarkerDescriptor = new DiagnosticDescriptor(
            "BLDYN0101",
            "Dynamic hash collection ghost serializer requires one supported interface",
            "Type '{0}' uses GhostDynamicHashMapAttribute but does not implement exactly one supported dynamic hash collection interface",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedCodecDescriptor = new DiagnosticDescriptor(
            "BLDYN0102",
            "Unsupported DynamicHashMap ghost codec",
            "Codec '{0}' is not supported for '{1}'. Use generated field codecs by default or DynamicHashMapRawGhostCodec<TKey, TValue> in RawStable mode.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedTypeDescriptor = new DiagnosticDescriptor(
            "BLDYN0103",
            "Unsupported DynamicHashMap ghost key or value type",
            "Type '{0}' is not supported by the generated DynamicHashMap ghost codec. " +
            "Use supported public-field unmanaged structs, primitives, or enums.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor MarkerFieldsDescriptor = new DiagnosticDescriptor(
            "BLDYN0104",
            "DynamicHashMap ghost marker must be byte-sized",
            "Type '{0}' must contain only the byte backing field for IDynamicHashMap<TKey, TValue>.Value",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor GenericMarkerDescriptor = new DiagnosticDescriptor(
            "BLDYN0105",
            "DynamicHashMap ghost marker cannot be generic",
            "Type '{0}' is generic. DynamicHashMap ghost serializer generation requires a concrete marker type.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedFieldDescriptor = new DiagnosticDescriptor(
            "BLDYN0107",
            "Unsupported DynamicHashMap ghost field",
            "Field '{1}' on type '{0}' cannot be serialized by the generated DynamicHashMap ghost codec. Fields must be public, writable, and supported.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor ExplicitLayoutDescriptor = new DiagnosticDescriptor(
            "BLDYN0108",
            "Explicit-layout DynamicHashMap ghost field type is unsupported",
            "Type '{0}' uses explicit or field-offset layout, which is not supported by generated DynamicHashMap ghost codecs. " +
            "Use sequential fields or RawStable.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor UnsupportedStorageDescriptor = new DiagnosticDescriptor(
            "BLDYN0109",
            "Unsupported DynamicHashMap ghost instance storage",
            "Instance storage '{1}' on type '{0}' cannot be serialized by the generated DynamicHashMap ghost codec. " +
            "Generated codecs support public writable instance fields only.",
            Category,
            DiagnosticSeverity.Error,
            true);

        internal static readonly DiagnosticDescriptor AmbiguousMarkerDescriptor = new DiagnosticDescriptor(
            "BLDYN0111",
            "Dynamic hash collection ghost serializer marker is ambiguous",
            "Type '{0}' must implement exactly one closed IDynamicHashMap<TKey, TValue> or IDynamicMultiHashMap<TKey, TValue> interface",
            Category,
            DiagnosticSeverity.Error,
            true);

        public static Diagnostic UnsupportedMarker(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedMarkerDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedCodec(INamedTypeSymbol typeSymbol, ITypeSymbol codecSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedCodecDescriptor,
                location ?? typeSymbol.Locations[0],
                codecSymbol?.ToDisplayString(DynamicGenerator.ShortTypeFormat) ?? "<missing>",
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedType(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedTypeDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic MarkerFields(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                MarkerFieldsDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic GenericMarker(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                GenericMarkerDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedField(ITypeSymbol typeSymbol, IFieldSymbol fieldSymbol, Location location)
        {
            return Diagnostic.Create(
                UnsupportedFieldDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat),
                fieldSymbol.Name);
        }

        public static Diagnostic ExplicitLayout(ITypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                ExplicitLayoutDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }

        public static Diagnostic UnsupportedStorage(ITypeSymbol typeSymbol, string storageName, Location location)
        {
            return Diagnostic.Create(
                UnsupportedStorageDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat),
                storageName);
        }

        public static Diagnostic AmbiguousMarker(INamedTypeSymbol typeSymbol, Location location)
        {
            return Diagnostic.Create(
                AmbiguousMarkerDescriptor,
                location ?? typeSymbol.Locations[0],
                typeSymbol.ToDisplayString(DynamicGenerator.ShortTypeFormat));
        }
    }
}
