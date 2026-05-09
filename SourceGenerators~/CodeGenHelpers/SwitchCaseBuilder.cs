using System;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public sealed class SwitchCaseBuilder : IBuilder
    {
        private SwitchBuilder _parent { get; }
        private string _case { get; }
        private bool _default { get; }
        private Action<ICodeWriter>? _content;

        public SwitchCaseBuilder(SwitchBuilder parent, string @case, bool @default = false)
        {
            _parent = parent;
            _case = @case;
            _default = @default;
        }

        public SwitchBuilder WithContent(Action<ICodeWriter> contentDelegate)
        {
            _content = contentDelegate ?? EmptyBody;
            return _parent;
        }

        public SwitchBuilder WithExpression(string returnValue = "null")
        {
            _content = w => w.AppendLine($"{_case} => {returnValue},");
            return _parent;
        }

        void IBuilder.Write(in CodeWriter writer)
        {
            if (_parent.Expression)
            {
                _content?.Invoke(writer);
            }
            else
            {
                writer.AppendLine(_default ? "default:" : $"case {_case}:");
                writer.IncreaseIndent();
                _content?.Invoke(writer);
                writer.AppendLine("break;");
                writer.DecreaseIndent();
            }
        }

        private static void EmptyBody(ICodeWriter writer) { }
    }
}
