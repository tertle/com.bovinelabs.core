using System.Collections.Generic;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public sealed class SwitchBuilder
    {
        private ICodeWriter _writer { get; }
        private string _switchOn { get; }
        internal bool Expression { get; }
        private readonly List<SwitchCaseBuilder> _switchCases = new List<SwitchCaseBuilder>();

        internal SwitchBuilder(ICodeWriter writer, string switchOn, bool expression)
        {
            _writer = writer;
            _switchOn = switchOn;
            Expression = expression;
        }

        public SwitchCaseBuilder AddCase(string @case)
        {
            var builder = new SwitchCaseBuilder(this, @case);
            _switchCases.Add(builder);
            return builder;
        }

        public ICodeWriter Close()
        {
            if (_writer is CodeWriter codeWriter)
            {
                if (Expression)
                {
                    WriterExpressionSwitchCase(codeWriter);
                }
                else
                {
                    WriteClassicSwitchCase(codeWriter);
                }
            }

            return _writer;
        }

        private void WriteClassicSwitchCase(CodeWriter writer)
        {
            using (_writer.Block($"switch({_switchOn})"))
            {
                foreach (IBuilder @case in _switchCases)
                {
                    @case.Write(writer);
                }
            }
        }

        private void WriterExpressionSwitchCase(CodeWriter writer)
        {
            using (_writer.BlockWithDelimiter($"return {_switchOn} switch"))
            {
                foreach (IBuilder @case in _switchCases)
                {
                    @case.Write(writer);
                }
            }
        }
    }
}
