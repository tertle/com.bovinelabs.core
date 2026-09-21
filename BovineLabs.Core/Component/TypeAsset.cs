namespace BovineLabs.Core
{
    using System;
    using UnityEngine;

    [CreateAssetMenu(menuName = "BovineLabs/Components/Type", fileName = "Type")]
    public class TypeAsset : ScriptableObject
    {
        public const string SearchProviderType = "types";

        [SerializeField]
        private string _typeName;

        public virtual Type ResolveType()
        {
            if (string.IsNullOrWhiteSpace(_typeName))
            {
                throw new InvalidOperationException($"{GetType().Name} '{name}' does not have a type assigned.");
            }

            return Type.GetType(_typeName) ?? throw new TypeLoadException(
                $"{GetType().Name} '{name}' could not resolve type '{_typeName}'. " +
                "The type may have been renamed, moved to another assembly, or removed.");
        }
    }
}
