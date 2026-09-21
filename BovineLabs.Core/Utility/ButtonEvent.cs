namespace BovineLabs.Core.Utility
{
    public struct ButtonEvent
    {
        public bool Value;

        public bool TryConsume()
        {
            if (Value)
            {
                Value = false;
                return true;
            }

            return false;
        }

        public bool TryProduce(bool value = true)
        {
            if (value && !Value)
            {
                Value = true;
                return true;
            }

            return false;
        }
    }
}
