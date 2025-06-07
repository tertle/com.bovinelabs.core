using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0008
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers.Internals
{
    internal static class SymbolHelpers
    {
        private static readonly Dictionary<string, string> _fullNamesMaping = new Dictionary<string, string>
            (StringComparer.OrdinalIgnoreCase)
            {
                { "string",     typeof(string).ToString()  },
                { "long",       typeof(long).ToString()    },
                { "int",        typeof(int).ToString()     },
                { "short",      typeof(short).ToString()   },
                { "ulong",      typeof(ulong).ToString()   },
                { "uint",       typeof(uint).ToString()    },
                { "ushort",     typeof(ushort).ToString()  },
                { "byte",       typeof(byte).ToString()    },
                { "double",     typeof(double).ToString()  },
                { "float",      typeof(float).ToString()   },
                { "decimal",    typeof(decimal).ToString() },
                { "bool",       typeof(bool).ToString()    },
            };

        public static string GetGloballyQualifiedTypeName(INamespaceOrTypeSymbol symbol)
        {
            var value = GetFullMetadataName(symbol);
            if(_fullNamesMaping.Any(x => x.Value == value))
            {
                return _fullNamesMaping.First(x => x.Value == value).Key;
            }
            else if(_fullNamesMaping.Any(x => x.Value == $"System.Nullable`1[{value}]"))
            {
                return _fullNamesMaping.First(x => x.Value == $"System.Nullable`1[{value}]").Key + "?";
            }

            return value;
        }

        public static string GetFullMetadataName(INamespaceOrTypeSymbol symbol)
        {
            ISymbol s = symbol;
            var sb = new StringBuilder(s.MetadataName);

            var last = s;
            s = s.ContainingSymbol;

            if (s == null)
            {
                return GetFullName(symbol);
            }

            while (!IsRootNamespace(s))
            {
                if (s is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }
                sb.Insert(0, s.MetadataName);

                s = s.ContainingSymbol;
            }

            var namedType = symbol as INamedTypeSymbol;

            if (namedType?.TypeArguments.Any() ?? false)
            {
                var genericArgs = string.Join(",", namedType.TypeArguments.Select(GetFullMetadataName));
                sb.Append($"[{ genericArgs }]");
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol s)
        {
            return s is INamespaceSymbol symbol && symbol.IsGlobalNamespace;
        }

        public static string GetFullName(INamespaceOrTypeSymbol type)
        {
            if (type is IArrayTypeSymbol arrayType)
            {
                return $"{GetFullName(arrayType.ElementType)}[]";
            }

            if (IsNullable((ITypeSymbol)type, out var t) && t != null)
            {
                return $"System.Nullable`1[{GetFullName(t)}]";
            }

            var name = type.ToDisplayString() ?? string.Empty;

            if (!_fullNamesMaping.TryGetValue(name, out string output))
            {
                output = name;
            }

            return output;
        }

        public static string GetQualifiedTypeName(ITypeSymbol typeSymbol)
        {
            var type = typeSymbol.GetTypeName();
            if (!type.Contains(".") || typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return type;
            }

            var @namespace = namedTypeSymbol.ContainingNamespace.ToString();
            var typeName = namedTypeSymbol.Name;

            if (typeSymbol.ContainingType is not null)
            {
                return $"{GetQualifiedTypeName(typeSymbol.ContainingType)}.{typeSymbol.Name}";
            }

            var fullyQualifiedName = $"{@namespace}.{typeName}";

            if (namedTypeSymbol.IsGenericType)
            {
                var generics = namedTypeSymbol.TypeArguments.Select(x => GetQualifiedTypeName(x));
                var baseName = fullyQualifiedName;
                var nullable = string.Empty;
                if (baseName.EndsWith("?", StringComparison.InvariantCulture))
                {
                    baseName = baseName.Substring(0, baseName.Length - 1);
                    nullable = "?";
                }

                fullyQualifiedName = $"{baseName}<{string.Join(", ", generics)}>{nullable}";
            }

            if (fullyQualifiedName.Contains(".") && !fullyQualifiedName.StartsWith("global::", StringComparison.InvariantCulture))
            {
                fullyQualifiedName = $"global::{fullyQualifiedName}";
            }

            return fullyQualifiedName;
        }

        public static bool IsNullable(ITypeSymbol type)
        {
            if (type.NullableAnnotation == NullableAnnotation.Annotated)
                return true;

            return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
                && type.OriginalDefinition.ToDisplayString().Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsNullable(ITypeSymbol type, out ITypeSymbol? nullableType)
        {
            if (IsNullable(type))
            {
                nullableType = type.NullableAnnotation == NullableAnnotation.Annotated ? type : ((INamedTypeSymbol)type).TypeArguments.First();
                return true;
            }
            else
            {
                nullableType = null;
                return false;
            }
        }
    }
}
