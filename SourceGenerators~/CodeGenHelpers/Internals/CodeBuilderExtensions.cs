using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers.Internals
{
    internal static class CodeBuilderExtensions
    {
        private static readonly Dictionary<string, string> _mappings = new Dictionary<string, string>
        {
            { "Boolean", "bool" },
            { "Byte", "byte" },
            { "SByte", "sbyte" },
            { "Char", "char" },
            { "Decimal", "decimal" },
            { "Double", "double" },
            { "Single", "float" },
            { "Int32", "int" },
            { "UInt32", "uint" },
            { "Int64", "long" },
            { "UInt64", "ulong" },
            { "Int16", "short" },
            { "UInt16", "ushort" },
            { "Object", "object" },
            { "String", "string" }
        };

        public static string GetTypeName(this ITypeSymbol symbol)
        {
            if (symbol.ContainingNamespace.Name == "System" && _mappings.ContainsKey(symbol.Name))
                return _mappings[symbol.Name];

            return SymbolHelpers.GetFullMetadataName(symbol);
        }

        public static string GetTypeName(this Type type)
        {
            if (type.Namespace == "System" && _mappings.ContainsKey(type.Name))
                return _mappings[type.Name];

            return type.FullName;
        }
    }
}
