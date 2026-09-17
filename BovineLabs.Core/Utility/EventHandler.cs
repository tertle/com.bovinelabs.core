namespace BovineLabs.Core.Utility
{
    public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs args);
}
