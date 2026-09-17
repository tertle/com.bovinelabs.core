namespace BovineLabs.Core.Utility
{
    using System;

    public static class HalfSizeTriangleMatrix
    {
        /// <summary>
        /// Sources: https://www.codeguru.com/cplusplus/tip-half-size-triangular-matrix/ and
        /// https://stackoverflow.com/questions/3187957/how-to-store-a-symmetric-matrix/.
        /// </summary>
        public static int GetIndex(int row, int column, int n)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (row >= n)
            {
                throw new ArgumentException($"row {row} >= n {n}", nameof(row));
            }

            if (column >= n)
            {
                throw new ArgumentException($"column {column} >= n {n}", nameof(column));
            }
#endif

            return row <= column ? (((row * n) - (((row - 1) * row) / 2)) + column) - row : (((column * n) - (((column - 1) * column) / 2)) + row) - column;
        }
    }
}
