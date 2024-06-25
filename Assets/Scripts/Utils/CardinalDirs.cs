using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    /// <summary>
    /// Represents a collection of four items, each assigned to one of the cardinal directions.
    /// </summary>
    public struct CardinalDirs<T> : IEnumerable<T>
    {
        public T N;
        public T E;
        public T S;
        public T W;

        public CardinalDirs(T n, T e, T s, T w)
        {
            N = n;
            E = e;
            S = s;
            W = w;
        }

        /// <summary>
        /// Get or set the item assigned to the 'index'th direction going clockwise, starting from the north at 0.
        /// </summary>
        /// <param name="index">Any integer.</param>
        public T this[int index]
        {
            readonly get => MathUtils.Mod(index, 4) switch
            {
                0 => N,
                1 => E,
                2 => S,
                3 => W,
                _ => throw new IndexOutOfRangeException() // this will never happen
            };
            set
            {
                switch (MathUtils.Mod(index, 4))
                {
                    case 0:
                        N = value;
                        break;
                    case 1:
                        E = value;
                        break;
                    case 2:
                        S = value;
                        break;
                    case 3:
                        W = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Returns a new <see cref="CardinalDirs{TResult}"/>, each element being the result of applying 'map' to the corresponding element of this collection.
        /// </summary>
        public readonly CardinalDirs<TResult> Map<TResult>(Func<T, TResult> map) => new(map(N), map(E), map(S), map(W));

        /// <summary>
        /// Returns a new <see cref="CardinalDirs{T}"/> obtained by rotating this one by 'steps' 90 degree rotations clockwise.
        /// </summary>
        public readonly CardinalDirs<T> Rotated(int steps) =>
            new(this[-steps], this[1 - steps], this[2 - steps], this[3 - steps]);

        /// <summary>
        /// Returns a new <see cref="CardinalDirs{T}"/> obtained by rotating this one by 'steps' 90 degree rotations clockwise, also rotating each element using 'rotate'.
        /// </summary>
        public readonly CardinalDirs<T> Rotated(int steps, Func<T, int, T> rotate) =>
            new(rotate(this[-steps], steps), rotate(this[1 - steps], steps),
                rotate(this[2 - steps], steps), rotate(this[3 - steps], steps));

        /// <summary>
        /// Returns a new <see cref="CardinalDirs{T}"/> obtained by switching the east and west directions.
        /// </summary>
        public readonly CardinalDirs<T> Flipped() => new(N, W, S, E);

        /// <summary>
        /// Returns a new <see cref="CardinalDirs{T}"/> obtained by switching the east and west directions, also flipping each element using 'flip'.
        /// </summary>
        public readonly CardinalDirs<T> Flipped(Func<T, T> flip) => new(flip(N), flip(W), flip(S), flip(E));

        public readonly IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < 4; i++)
                yield return this[i];
        }

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}