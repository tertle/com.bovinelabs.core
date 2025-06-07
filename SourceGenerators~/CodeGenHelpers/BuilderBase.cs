using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0008
#pragma warning disable IDE0090
#nullable enable
namespace CodeGenHelpers
{
    public abstract class BuilderBase : IBuilder
    {
        protected readonly List<string> _pragmaWarnings = new List<string>();

        internal abstract void Write(in CodeWriter writer);

        void IBuilder.Write(in CodeWriter writer)
        {
            var warnings = string.Join(", ", _pragmaWarnings.Distinct());

            if(_pragmaWarnings.Any())
            {
                writer.AppendUnindentedLine($"#pragma warning disable {warnings}");
            }

            Write(writer);

            if (_pragmaWarnings.Any())
            {
                writer.AppendUnindentedLine($"#pragma warning restore {warnings}");
            }
        }
    }
}
