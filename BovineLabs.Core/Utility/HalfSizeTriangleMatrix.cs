// <copyright file="HalfSizeTriangleMatrix.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;

    public static class HalfSizeTriangleMatrix
    {
        /// <summary> Gets the index inside a half sized triangle matrix. </summary>
        /// <remarks>
        /// 0 1 2 3
        /// 4 5 6
        /// 7 8
        /// 9
        /// Sourced from
        /// https://www.codeguru.com/cplusplus/tip-half-size-triangular-matrix/
        /// https://stackoverflow.com/questions/3187957/how-to-store-a-symmetric-matrix/.
        /// </remarks>
        /// <param name="row"> First index. </param>
        /// <param name="column"> Second index. </param>
        /// <param name="n"> Size of the matrix (elements per side). </param>
        /// <returns> The index in the triangle matrix. </returns>
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
