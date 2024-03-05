using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public interface IReadOnlyArray2D<T> : IReadOnlyCollection<T>
    {
        public Vector2Int Size { get; }
        /// <summary>
        /// Is the given index within bounds of the array?
        /// </summary>
        bool IsInBounds(Vector2Int index);
        /// <summary>
        /// Returns the entry at the given index. Index out of bounds leads to undefined behavior.
        /// </summary>
        T this[Vector2Int index] { get; }
        /// <summary>
        /// Returns the entry at the given index. Index out of bounds leads to undefined behavior.
        /// </summary>
        T this[int x, int y] { get; }
        /// <summary>
        /// Gets the entry at the given index, if the index is in bounds of the array.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The entry at the given index, if the index is in bounds, otherwise the default value for this type.</param>
        /// <returns>true if the index was in bounds, otherwise false</returns>
        bool TryGet(Vector2Int index, out T value);
        /// <summary>
        /// Returns the entry at the given index, wrapping around if the index is out of bounds.
        /// </summary>
        T GetWrapping(Vector2Int index);
        /// <summary>
        /// Returns the entry at the given index or 'defaultValue' if the index is out of bounds.
        /// </summary>
        T GetOrDefault(Vector2Int index, T defaultValue);
        /// <summary>
        /// Copies this array to the provided array.
        /// </summary>
        void CopyTo(T[] array);
        /// <summary>
        /// Creates a shallow copy of the array.
        /// </summary>
        Array2D<T> Clone();
        /// <summary>
        /// Provides the entries in a sequential order along with their index.
        /// </summary>
        IEnumerable<(Vector2Int index, T value)> IndexedEnumerable { get; }
    }
    /// <summary>
    /// A 2D array, faster than T[,] and more practical than T[][].
    /// </summary>
    [Serializable]
    public class Array2D<T> : IReadOnlyArray2D<T>
    {
        readonly T[] array_;
        public Vector2Int Size { get; }
        /// <summary>
        /// Creates a new empty array of the given size.
        /// </summary>
        public Array2D(Vector2Int size)
        {
            Size = size;
            array_ = new T[Count];
        }
        /// <summary>
        /// Creates a new array of the given size, using the provided array to store its values.
        /// </summary>
        public Array2D(T[] array, Vector2Int size)
        {
            array_ = array;
            Size = size;
        }
        /// <summary>
        /// Puts value at each index in the array.
        /// </summary>
        public void Fill(T value)
        {
            foreach (var i in Size)
            {
                this[i] = value;
            }
        }
        /// <summary>
        /// Puts a result of getValue at each index in the array.
        /// </summary>
        public void Fill(Func<T> getValue)
        {
            foreach (var i in Size)
            {
                this[i] = getValue();
            }
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)array_).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// The width of the array multiplied by its height - the number of entries it can hold.
        /// </summary>
        public int Count => Size.x * Size.y;
        public bool IsInBounds(Vector2Int index) => index.x >= 0 && index.x < Size.x && index.y >= 0 && index.y < Size.y;
        /// <summary>
        /// Gets or sets the entry at the given index. Index out of bounds leads to undefined behavior.
        /// </summary>
        public T this[Vector2Int index]
        {
            get => this[index.x, index.y];
            set => this[index.x, index.y] = value;
        }
        /// <summary>
        /// Gets or sets the entry at the given index. Index out of bounds leads to undefined behavior.
        /// </summary>
        public T this[int x, int y]
        {
            get => array_[x + Size.x * y];
            set => array_[x + Size.x * y] = value;
        }

        public bool TryGet(Vector2Int index, out T value)
        {
            bool ret = IsInBounds(index);
            value = ret ? this[index] : default;
            return ret;
        }
        /// <summary>
        /// Sets the entry at the given index, if the index is in bounds of the array.
        /// </summary>
        /// <returns>true if the index was in bounds, otherwise false</returns>
        public bool TrySet(Vector2Int index, T value)
        {
            if (!IsInBounds(index))
                return false;
            this[index] = value;
            return true;
        }

        public T GetWrapping(Vector2Int index) => this[MathUtils.Mod(index.x, Size.x), MathUtils.Mod(index.y, Size.y)];

        public T GetOrDefault(Vector2Int index, T defaultValue) => IsInBounds(index) ? this[index] : defaultValue;

        public void CopyTo(T[] array) => Array.Copy(array_, 0, array, 0, Count);

        public Array2D<T> Clone() => new((T[])array_.Clone(), Size);
        public IEnumerable<(Vector2Int index, T value)> IndexedEnumerable => new Indexed(this);

        class Indexed : IEnumerable<(Vector2Int index, T value)>
        {
            readonly Array2D<T> array_;

            public Indexed(Array2D<T> array)
            {
                array_ = array;
            }
            public IEnumerator<(Vector2Int index, T value)> GetEnumerator()
            {
                int x = 0, y = 0;
                foreach (var value in array_)
                {
                    yield return (new(x, y), value);
                    x++;
                    if (x != array_.Size.x)
                        continue;
                    y++;
                    x = 0;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
