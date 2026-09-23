namespace BovineLabs.Core
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Unmanaged tuple values for Burst code, including CoreCLR builds.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Tu<T1>
        where T1 : unmanaged
    {
        public T1 Item1;

        public Tu(T1 item1)
        {
            Item1 = item1;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Tu<T1, T2>
        where T1 : unmanaged
        where T2 : unmanaged
    {
        public T1 Item1;
        public T2 Item2;

        public Tu(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public void Deconstruct(out T1 item1, out T2 item2)
        {
            item1 = Item1;
            item2 = Item2;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Tu<T1, T2, T3>
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public Tu(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = Item1;
            item2 = Item2;
            item3 = Item3;
        }
    }
}
