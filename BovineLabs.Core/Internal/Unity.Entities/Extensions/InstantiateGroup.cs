namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;

    /// <summary>One prefab's instances for <see cref="EntityManagerExtensions.InstantiateExisting{T}"/>.</summary>
    public struct InstantiateGroup<T>
        where T : unmanaged, ISharedComponentData
    {
        /// <summary>The prefab, and its <see cref="LinkedEntityGroup"/>, to instantiate.</summary>
        public Entity Prefab;

        /// <summary>The first of the group's reserved root entities.</summary>
        public int Start;

        /// <summary>The number of instances.</summary>
        public int Count;

        /// <summary>The value of <typeparamref name="T"/> for every instance entity that has it, when <see cref="SetValue"/> is set.</summary>
        public T Value;

        /// <summary>Whether to give the instances <see cref="Value"/>; otherwise they keep the prefab's values.</summary>
        public bool SetValue;
    }
}
