namespace BovineLabs.Core.Editor.Inspectors
{
    using Unity.Entities.Editor;
    using UnityEngine.UIElements;

    public abstract class EntityInspector<T> : VisualElement
    {
        protected EntityInspector(object inspector)
        {
            Context = ContextGetter.Create<T>(inspector);
        }

        internal IContextGetter Context { get; }

        public virtual bool IsValid()
        {
            return Context.World.IsCreated && Context.EntityManager.SafeExists(Context.Entity);
        }
    }
}
