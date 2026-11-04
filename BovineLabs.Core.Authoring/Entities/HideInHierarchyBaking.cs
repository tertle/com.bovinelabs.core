namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Entities;

    /// <summary>
    /// Hides baked entities from the Entities hierarchy, whose cost grows with the entity count, when baking for the Editor. Player builds are unchanged.
    /// </summary>
    public static class HideInHierarchyBaking
    {
        public static void HideInEditorHierarchy(this IBaker baker, Entity entity)
        {
            if (baker.IsBakingForEditor())
            {
                baker.AddComponent<HideInHierarchyRequest>(entity);
            }
        }
    }
}
