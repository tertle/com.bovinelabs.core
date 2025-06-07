using System.Collections.Generic;
using System.Linq;

namespace CodeGenHelpers
{
    internal class GenericCollection : List<GenericBuilder>
    {
        public override string ToString()
        {
            if (!this.Any())
                return string.Empty;

            return $"<{string.Join(", ", this.Select(x => x.Name))}>";
        }

        public string[] Contraints() =>
            this.Select(x => x.ToString())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
    }
}
