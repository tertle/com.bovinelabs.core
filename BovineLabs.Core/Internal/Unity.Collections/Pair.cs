namespace BovineLabs.Core.Internal
{
    internal struct Pair<TKey, TValue>
    {
        public TKey key;
        public TValue value;

        public Pair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }

        public override string ToString() => $"{this.key} = {this.value}";
    }
}
