using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable IDE0008
#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    internal sealed class CodeWriter : IDisposable, ICodeWriter
    {
        private readonly IndentStyle _indentStyle;
        private readonly string Indent;

        private string _delimiter = string.Empty;
        private int _indentLevel = 0;
        private int _extraIndent = 0;
        private readonly List<(StringBuilder? StringBuilder, CodeWriter? Writer)> _blocks = new();

        public CodeWriter(IndentStyle indentStyle, int startingLevel = 0)
        {
            _indentStyle = indentStyle;
            Indent = indentStyle switch
            {
                IndentStyle.Tabs => "\t",
                _ => "    "
            };
        }

        public void IncreaseIndent() => _extraIndent++;

        public void DecreaseIndent() => _extraIndent--;

        public IDisposable Block(string value, params string[] constraints)
        {
            _delimiter = string.Empty;

            AppendLine(value.TrimEnd());
            _indentLevel++;
            foreach (var constraint in constraints)
            {
                if (string.IsNullOrEmpty(constraint))
                    continue;

                AppendLine(constraint);
            }

            _indentLevel--;
            AppendLine("{");
            _indentLevel++;
            return this;
        }

        public IDisposable BlockWithDelimiter(string value, params string[] constraints)
        {
            var dis = Block(value, constraints);
            _delimiter = ";";

            return dis;
        }

        public ICodeWriter BlockWriter(string? originalLine)
        {
            var writer = new CodeWriter(_indentStyle, _indentLevel);
            if (originalLine is { })
            {
                writer.AppendLine(originalLine);
            }
            writer.AppendLine("{");
            writer._indentLevel++;
            _blocks.Add((null, writer));
            return writer;
        }

        private StringBuilder EnsureStringBuilder()
        {
            if (_blocks.LastOrDefault().StringBuilder is not { } sb)
            {
                sb = new StringBuilder();
                _blocks.Add((sb, null));
            }

            return sb;
        }

        public void Append(string value)
        {
            EnsureStringBuilder().Append(GetIndentedValue(value.TrimEnd()));
        }

        public void AppendUnindented(string value)
        {
            EnsureStringBuilder().Append(value.TrimEnd());
        }

        public void NewLine()
        {
            EnsureStringBuilder().AppendLine();
        }

        public void AppendLine(string value)
        {
            EnsureStringBuilder().AppendLine(GetIndentedValue(value.TrimEnd()));
        }

        public void AppendUnindentedLine(string value)
        {
            EnsureStringBuilder().AppendLine(value.TrimEnd());
        }

        private string GetIndentedValue(string value)
        {
            var indent = string.Empty;
            for (var i = 0; i < _indentLevel + _extraIndent; i++)
                indent += Indent;

            return indent + value;
        }

        public void Dispose()
        {
            if (_indentLevel > 0)
            {
                _indentLevel--;
                EnsureStringBuilder().AppendLine(GetIndentedValue($"}}{this._delimiter}"));
            }

            _delimiter = string.Empty;
        }

        public string Render()
        {
            var result = new StringBuilder();

            foreach (var block in _blocks)
            {
                if (block.StringBuilder is { })
                {
                    result.Append(block.StringBuilder);
                }

                if (block.Writer is { })
                {
                    block.Writer.Dispose();
                    result.Append(block.Writer.Render());
                }
            }

            Dispose();

            return result.ToString();
        }
    }
}
