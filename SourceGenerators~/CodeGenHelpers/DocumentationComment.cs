using System;
using System.Linq;
using System.Collections.Generic;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    internal abstract class DocumentationComment
    {
        public abstract void Write(in CodeWriter writer);
    }

    internal class InheritDocumentationComment : DocumentationComment
    {
        internal string? InheritFrom { get; set; }

        public override void Write(in CodeWriter writer)
        {
            writer.AppendLine($"/// <inheritdoc {(InheritFrom is null ? string.Empty : $"cref=\"{InheritFrom}\"")}/>");
        }
    }

    internal class SummaryDocumentationComment : DocumentationComment
    {
        internal string? Summary { get; set; }

        public override void Write(in CodeWriter writer)
        {
            if (Summary is null || string.IsNullOrEmpty(Summary))
                return;

            writer.AppendLine("/// <summary>");

            string[] lines = Summary.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
                writer.AppendLine($"/// {line}");

            writer.AppendLine("/// </summary>");
        }
    }

    internal class ParameterDocumentationComment : SummaryDocumentationComment
    {
        private readonly Dictionary<string, string> parameterDocs = new Dictionary<string, string>();

        public void AddParameter(string parameterName, string documentationComment)
        {
            parameterDocs[parameterName] = documentationComment;
        }

        internal void RemoveUnusedParameters<T>(List<ParameterBuilder<T>> parameters)
            where T : BuilderBase<T>, IParameterized<T>
        {
            var unusedParameters = parameterDocs.Where(p =>
                !parameters.Any(x => x.Name == p.Key)).ToArray();
            foreach (var parameter in unusedParameters)
                parameterDocs.Remove(parameter.Key);
        }

        public override void Write(in CodeWriter writer)
        {
            base.Write(writer);
            foreach (var param in parameterDocs.OrderBy(x => x.Key))
                writer.AppendLine($"/// <param name=\"{param.Key}\">{param.Value}</param>");
        }
    }
}
