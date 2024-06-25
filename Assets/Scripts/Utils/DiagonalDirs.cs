using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    /// <summary>
    /// Represents a collection of four items, each assigned to one of the diagonal directions (north-west, north-east, south-east and south-west).
    /// </summary>
    public struct DiagonalDirs<T> : IEnumerable<T>
    {
        public T NW;
        public T NE;
        public T SE;
        public T SW;

        public DiagonalDirs(T nw, T ne, T se, T sw)
        {
            NW = nw;
            NE = ne;
            SE = se;
            SW = sw;
        }

        /// <summary>
        /// Get or set the item assigned to the 'index'th direction going clockwise, starting from the north-west at 0.
        /// </summary>
        /// <param name="index">Any integer.</param>
        public T this[int index]
        {
            readonly get => MathUtils.Mod(index, 4) switch
            {
                0 => NW,
                1 => NE,
                2 => SE,
                3 => SW,
                _ => throw new IndexOutOfRangeException() // this will never happen
            };
            set
            {
                switch (MathUtils.Mod(index, 4))
                {
                    case 0:
                        NW = value;
                        break;
                    case 1:
                        NE = value;
                        break;
                    case 2:
                        SE = value;
                        break;
                    case 3:
                        SW = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="DiagonalDirs{TResult}"/>, each element being the result of applying 'map' to the corresponding element of this collection.
        /// </summary>
        public readonly DiagonalDirs<TResult> Map<TResult>(Func<T, TResult> map) => new(map(NW), map(NE), map(SE), map(SW));

        /// <summary>
        /// Returns a new <see cref="DiagonalDirs{T}"/> obtained by rotating this one by 'steps' 90 degree rotations clockwise.
        /// </summary>
        public readonly DiagonalDirs<T> Rotated(int steps) =>
            new(this[-steps], this[1 - steps], this[2 - steps], this[3 - steps]);

        /// <summary>
        /// Returns a new <see cref="DiagonalDirs{T}"/> obtained by rotating this one by 'steps' 90 degree rotations clockwise, also rotating each element using 'rotate'.
        /// </summary>
        public readonly DiagonalDirs<T> Rotated(int steps, Func<T, int, T> rotate) =>
            new(rotate(this[-steps], steps), rotate(this[1 - steps], steps),
                rotate(this[2 - steps], steps), rotate(this[3 - steps], steps));

        /// <summary>
        /// Returns a new <see cref="DiagonalDirs{T}"/> obtained by switching the east and west directions.
        /// </summary>
        public readonly DiagonalDirs<T> Flipped() => new(NE, NW, SW, SE);

        /// <summary>
        /// Returns a new <see cref="DiagonalDirs{T}"/> obtained by switching the east and west directions, also flipping each element using 'flip'.
        /// </summary>
        public readonly DiagonalDirs<T> Flipped(Func<T, T> flip) => new(flip(NE), flip(NW), flip(SW), flip(SE));

        public readonly IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 4; i++)
                yield return this[i];
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}