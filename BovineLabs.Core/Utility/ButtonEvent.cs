namespace BovineLabs.Core.Utility
{
    public struct ButtonEvent
    {
        public bool Value;

        public bool TryConsume()
        {
            if (this.Value)
            {
                this.Value = false;
                return true;
            }

            return false;
        }

        public bool TryProduce(bool value = true)
        {
            if (value && !this.Value)
            {
                this.Value = true;
                return true;
            }

            return false;
        }
    }
}
