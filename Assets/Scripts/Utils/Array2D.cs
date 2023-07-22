using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public interface IReadOnlyArray2D<T> : IReadOnlyCollection<T>
    {
        bool IsInBounds(Vector2Int index);
        T this[Vector2Int index] { get; }
        T this[int x, int y] { get; }
        bool TryGet(Vector2Int index, out T value);
        T GetWrapping(Vector2Int index);
        T GetOrDefault(Vector2Int index, T defaultValue);
        void CopyTo(T[] array);
        Array2D<T> Clone();
        IEnumerable<(Vector2Int index, T value)> IndexedEnumerable { get; }
    }
    [Serializable]
    public class Array2D<T> : IReadOnlyArray2D<T>
    {
        readonly T[] array_;
        public Vector2Int Size { get; }

        public Array2D(Vector2Int size)
        {
            Size = size;
            array_ = new T[Count];
        }
        public Array2D(T[] array, Vector2Int size)
        {
            array_ = array;
            Size = size;
        }
        public Array2D(IEnumerable<IEnumerable<T>> source, Vector2Int size)
        {
            Size = size;
            array_ = new T[Count];
            int i = 0;
            foreach (var row in source)
            {
                foreach (var value in row)
                {
                    array_[i++] = value;
                }
            }
        }
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)array_).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => Size.x * Size.y;
        public bool IsInBounds(Vector2Int index) => index.x >= 0 && index.x < Size.x && index.y >= 0 && index.y < Size.y;

        public T this[Vector2Int index]
        {
            get => this[index.x, index.y];
            set => this[index.x, index.y] = value;
        }

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

        public T GetWrapping(Vector2Int index) => this[MathUtils.Mod(index.x, Size.x), MathUtils.Mod(index.y, Size.y)];

        public T GetOrDefault(Vector2Int index, T defaultValue) => IsInBounds(index) ? this[index] : defaultValue;

        public void CopyTo(T[] array) => Array.Copy(array_, 0, array, 0, Count);

        public Array2D<T> Clone() => new(array_, Size);
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
