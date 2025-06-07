using System.Collections.Generic;
using System.Linq;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0008
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public static class ParameterizedExtensions
    {
        public static T AddParameter<T>(this IParameterized<T> parameterized, string typeName)
            where T : BuilderBase<T>, IParameterized<T>
        {
            return parameterized.AddParameterInternal(GetParameter(parameterized.Parent, typeName, null), -1);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameter(typeSymbol.Name);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, string typeName, string parameterName)
            where T : BuilderBase<T>, IParameterized<T>
        {
            return parameterized.AddParameterInternal(GetParameter(parameterized.Parent, typeName, parameterName), -1);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol, string parameterName)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameter(SymbolHelpers.GetGloballyQualifiedTypeName(typeSymbol), parameterName);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, string typeName, string parameterName, int index)
            where T : BuilderBase<T>, IParameterized<T>
        {
            return parameterized.AddParameterInternal(GetParameter(parameterized.Parent, typeName, parameterName), index);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol, string parameterName, int index)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameter(typeSymbol.Name, parameterName, index);
        }

        public static T AddParameterWithDefaultValue<T>(this IParameterized<T> parameterized, string typeName, string? parameterName = null, object? defaultValue = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            var parameter = GetParameter(parameterized.Parent, typeName, parameterName)
                .WithDefaultValue(defaultValue);
            return parameterized.AddParameterInternal(parameter, index);
        }

        public static T AddParameterWithDefaultValue<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol, string? parameterName = null, object? defaultValue = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameterWithDefaultValue(typeSymbol.Name, parameterName, defaultValue, index);
        }

        public static T AddParameterWithDefaultValue<T>(this IParameterized<T> parameterized, string typeName, string? parameterName = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            var parameter = GetParameter(parameterized.Parent, typeName, parameterName)
                .WithDefaultValue();
            return parameterized.AddParameterInternal(parameter, index);
        }

        public static T AddParameterWithDefaultValue<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol, string? parameterName = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameterWithDefaultValue(typeSymbol, parameterName, index);
        }

        public static T AddParameterWithNullValue<T>(this IParameterized<T> parameterized, string typeName, string? parameterName = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            var parameter = GetParameter(parameterized.Parent, typeName, parameterName)
                .WithNullDefault();
            return parameterized.AddParameterInternal(parameter, index);
        }

        public static T AddParameterWithNullValue<T>(this IParameterized<T> parameterized, ITypeSymbol typeSymbol, string? parameterName = null, int index = -1)
            where T : BuilderBase<T>, IParameterized<T>
        {
            parameterized.Parent.AddNamespaceImport(typeSymbol);
            return parameterized.AddParameterWithNullValue(typeSymbol.Name, parameterName, index);
        }

        public static T AddParameter<T>(this IParameterized<T> parameterized, IParameterSymbol parameter)
            where T : BuilderBase<T>, IParameterized<T>
        {
            if (parameter.HasExplicitDefaultValue)
                return parameterized.AddParameterWithDefaultValue(parameter.Type, parameter.Name, parameter.ExplicitDefaultValue);

            return parameterized.AddParameter(parameter.Type, parameter.Name);
        }

        public static T AddParameters<T>(this IParameterized<T> parameterized, IEnumerable<IParameterSymbol> parameters)
            where T : BuilderBase<T>, IParameterized<T>
        {
            if (parameters is null || !parameters.Any())
                return parameterized.Parent;

            foreach (var parameter in parameters)
                parameterized.AddParameter(parameter);

            return parameterized.Parent;
        }

        private static T AddParameterInternal<T>(this IParameterized<T> parameterized, ParameterBuilder<T> parameter, int index)
            where T : BuilderBase<T>, IParameterized<T>
        {
            if (parameterized.Parameters.Any(p => p.Type == parameter.Type && p.Name == parameter.Name))
                return parameterized.Parent;

            if (index > -1)
            {
                parameterized.Parameters.Insert(index, parameter);
            }
            else
            {
                parameterized.Parameters.Add(parameter);
            }

            return parameterized.Parent;
        }

        private static ParameterBuilder<T> GetParameter<T>(T parent, string typeName, string? parameterName)
            where T : BuilderBase<T>
        {
            if (parameterName is null || string.IsNullOrEmpty(parameterName))
            {
                parameterName = char.ToLower(typeName[0]) + typeName.Substring(1);
            }

            return new ParameterBuilder<T>(parent)
            {
                Type = typeName,
                Name = parameterName
            };
        }
    }
}
