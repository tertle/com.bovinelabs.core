namespace BovineLabs.Core.Editor.ConfigVars
{
    using UnityEngine.UIElements;

    public interface IConfigVarBinding<T> : IBinding
    {
        T Value { get; }
    }
}
