using System;

namespace CodeGenHelpers
{
    public sealed class ExpressionBlockBuilder
    {
        private ICodeWriter _writer { get; }
        private string _blockCode { get; }

        internal ExpressionBlockBuilder(ICodeWriter writer, string block)
        {
            _writer = writer;
            _blockCode = block;
        }

        public ICodeWriter WithBody(Action<ICodeWriter> innerWrite)
        {
            using (_writer.Block(_blockCode))
            {
                innerWrite?.Invoke(_writer);
            }

            return _writer;
        }
    }
}