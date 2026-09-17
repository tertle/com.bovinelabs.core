namespace BovineLabs.Savanna
{
    using System;

    [AttributeUsage(AttributeTargets.Struct)]
    public class SaveAttribute : Attribute
    {
        public SaveAttribute(SaveFeature feature = SaveFeature.None)
        {
            this.Feature = feature;
        }

        public SaveFeature Feature { get; }
    }
}
