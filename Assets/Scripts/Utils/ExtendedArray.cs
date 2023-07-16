using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public interface IReadOnlyExtendedArray<out TValue, in TIndex> : IEnumerable<TValue>
    {
        public bool IsInBounds(TIndex index);
        public TValue this[TIndex index] { get; }
    }
    public interface IExtendedArray<TValue, in TIndex> : IReadOnlyExtendedArray<TValue, TIndex>
    {
        public new TValue this[TIndex index] { set; }
    }
    public class ExtendedArray<T> : IExtendedArray<T, int>
    {
        readonly T[] array_;
        public int Size { get; }
        readonly T defaultValue_;

        public ExtendedArray(T[] array, T defaultValue)
        {
            array_ = array;
            Size = array_.Length;
            defaultValue_ = defaultValue;
        }
        public ExtendedArray(int size, T defaultValue)
        {
            array_ = new T[size];
            Size = size;
            defaultValue_ = defaultValue;
        }

        public T At(int index) => this[index];

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Size; i++)
            {
                yield return At(i);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsInBounds(int index) => index >= 0 && index < Size;

        public T this[int index]
        {
            get => IsInBounds(index) ? array_[index] : defaultValue_;
            set { if (IsInBounds(index)) array_[index] = value; }
        }
    }

    public class ExtendedArray2D<T> : IExtendedArray<T, Vector2Int>
    {
        readonly T[,] array_;
        public Vector2Int Size { get; }
        readonly T defaultValue_;

        public ExtendedArray2D(T[,] array, T defaultValue)
        {
            array_ = array;
            Size = new(array_.GetLength(0), array_.GetLength(1));
            defaultValue_ = defaultValue;
        }
        public ExtendedArray2D(Vector2Int size, T defaultValue)
        {
            array_ = new T[size.x, size.y];
            Size = size;
            defaultValue_ = defaultValue;
        }

        public T At(Vector2Int index) => this[index];

        public IEnumerator<T> GetEnumerator()
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    yield return At(new(x, y));
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool IsInBounds(Vector2Int index) => index.x >= 0 && index.x < Size.x && index.y >= 0 && index.y < Size.y;

        public T this[Vector2Int index]
        {
            get => IsInBounds(index) ? array_[index.x, index.y] : defaultValue_;
            set { if (IsInBounds(index)) array_[index.x, index.y] = value; }
        }
    }
}
