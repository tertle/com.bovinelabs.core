namespace BovineLabs.Core.Internal
{
    using System.Collections.Generic;
    using Unity.Entities;

    public unsafe struct EntityArchetypeComparer : IComparer<EntityArchetype>
    {
        public int Compare(EntityArchetype x, EntityArchetype y)
        {
            if (x.Archetype == y.Archetype)
            {
                return 0;
            }

            return x.Archetype < y.Archetype ? -1 : 1;
        }
    }
}
