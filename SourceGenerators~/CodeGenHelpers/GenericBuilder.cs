using System.Collections.Generic;

namespace CodeGenHelpers
{
    public sealed class GenericBuilder
    {
        private List<string> _constraints { get; }
        private bool _isClass;
        private bool _isUnmanaged;
        private bool _newDefault;

        internal GenericBuilder(string name)
        {
            Name = name;
            _constraints = new List<string>();
        }

        public string Name { get; }

        public GenericBuilder New()
        {
            _newDefault = true;
            return this;
        }

        public GenericBuilder Class()
        {
            _isClass = true;
            return this;
        }

        public GenericBuilder Unmanaged()
        {
            _isUnmanaged = true;
            return this;
        }

        public GenericBuilder AddConstraint(string constraint)
        {
            switch (constraint)
            {
                case "new()": return New();
                case "class": return Class();
                case "unmanaged": return Unmanaged();
                default:
                    if (!(string.IsNullOrWhiteSpace(constraint) || _constraints.Contains(constraint)))
                        _constraints.Add(constraint);
                    break;
            }

            return this;
        }

        public override string ToString()
        {
            if (this._isUnmanaged)
            {
                _constraints.Insert(0, "unmanaged");
            }
            else
            {
                if (_isClass)
                    _constraints.Insert(0, "class");

                if (_newDefault)
                    _constraints.Insert(0, "new()");
            }

            var output = string.Join(", ", _constraints).Trim();
            if(string.IsNullOrEmpty(output))
                return string.Empty;

            return $"where {Name} : {output}";
        }
    }
}
