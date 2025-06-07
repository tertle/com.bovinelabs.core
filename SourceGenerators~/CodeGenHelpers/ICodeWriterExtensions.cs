using System;
using System.Collections.Generic;

namespace CodeGenHelpers
{
    public static class ICodeWriterExtensions
    {
        public static LogicalConditionBuilder If(this ICodeWriter writer, string condition)
        {
            return new LogicalConditionBuilder(writer, condition);
        }

        public static ExpressionBlockBuilder For(this ICodeWriter writer, string incrementerName = "i", int counterStart = 0, string condition = "i < 10", bool increment = true)
        {
            var incrementerType = increment ? "++" : "--";
            return new ExpressionBlockBuilder(writer, $"for ({incrementerName} = {counterStart}; {condition}; {incrementerName}{incrementerType}");
        }

        public static ExpressionBlockBuilder While(this ICodeWriter writer, string condition)
        {
            return new ExpressionBlockBuilder(writer, $"while ({condition})");
        }

        public static ExpressionBlockBuilder ForEach(this ICodeWriter writer, string parameter, string collection)
        {
            return new ExpressionBlockBuilder(writer, $"foreach ({parameter} in {collection})");
        }

        public static SwitchBuilder Switch(this ICodeWriter writer, string switchOn)
        {
            return new SwitchBuilder(writer, switchOn, false);
        }

        public static SwitchBuilder SwitchExpression(this ICodeWriter writer, string switchOn)
        {
            return new SwitchBuilder(writer, switchOn, true);
        }

        public static ExpressionBlockBuilder ExpressionBlock(this ICodeWriter writer, string expression)
        {
            return new ExpressionBlockBuilder(writer, expression);
        }

        public static ICodeWriter AppendLines<T>(this ICodeWriter writer, IEnumerable<T> collection, Func<T, string> predicate)
        {
            foreach (var value in collection)
                writer.AppendLine(predicate(value));

            return writer;
        }

        public static ICodeWriter AppendLines<TKey, TValue>(this ICodeWriter writer, Dictionary<TKey, TValue> collection, Func<TKey, TValue, string> predicate)
        {
            foreach (var pair in collection)
                writer.AppendLine(predicate(pair.Key, pair.Value));

            return writer;
        }
    }
}