using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils.Random
{
    /// <summary>
    /// Represents an unordered set of items of type T, being able to return them in a random order.
    /// </summary>
    public class RandomSet<T> : ICollection<T>
    {
        // underlying container of items
        readonly List<T> list_;
        // maps items to their position in list_
        readonly Dictionary<T, int> positions_;
        readonly Random random_;

        public int Count { get => list_.Count; }
        public bool IsReadOnly => false;

        /// <summary>
        /// Creates a new empty instance of <see cref="RandomSet{T}"/>.
        /// </summary>
        /// <param name="randomSeed">Seed for the random number generator.</param>
        public RandomSet(ulong randomSeed)
        {
            list_ = new();
            positions_ = new();
            random_ = new(randomSeed);
        }
        /// <summary>
        /// Creates a copy of another <see cref="RandomSet{T}"/>, copying both its contents and random number generator to provide items in the same order.
        /// </summary>
        /// <param name="original">The set to copy.</param>
        public RandomSet(RandomSet<T> original)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(original.random_.CurrentState);
        }
        /// <summary>
        /// Creates a new instance of <see cref="RandomSet{T}"/>, filled with the items from the provided container.
        /// </summary>
        /// <param name="items">The container to take items from.</param>
        /// <param name="randomSeed">Seed for the random number generator.</param>
        public RandomSet(IEnumerable<T> items, ulong randomSeed) : this(randomSeed)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        /// <summary>
        /// Adds a new item to the set, if not present.
        /// </summary>
        public void Add(T item)
        {
            if (positions_.ContainsKey(item))
                return;
            positions_.Add(item, list_.Count);
            list_.Add(item);
        }
        /// <summary>
        /// Copies the elements in an arbitrary order (not random) to an array, starting at the given array index.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex) => list_.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes an item from the set, if present.
        /// </summary>
        /// <returns>true, if the item was present, false otherwise.</returns>
        public bool Remove(T item)
        {
            if (!positions_.ContainsKey(item))
                return false;
            int pos = positions_[item];
            positions_.Remove(item);
            if (pos != list_.Count - 1)
            {
                list_[pos] = list_[^1];
                positions_[list_[pos]] = pos;
            }
            list_.RemoveAt(list_.Count - 1);
            return true;
        }
        /// <summary>
        /// Adds multiple items to the set, skipping those that are already present.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        /// <summary>
        /// Gets a random item from the set and removes it. Throws an exception when the set is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public T PopRandom()
        {
            if (list_.Count == 0)
                throw new InvalidOperationException("Cannot pop from an empty set");
            int r = random_.Int(list_.Count);
            T ret = list_[r];
            Remove(ret);
            return ret;
        }
        /// <summary>
        /// Tests whether an item is present in the set.
        /// </summary>
        public bool Contains(T item) => positions_.ContainsKey(item);

        /// <summary>
        /// Removes all items.
        /// </summary>
        public void Clear()
        {
            list_.Clear();
            positions_.Clear();
        }

        public IEnumerator<T> GetEnumerator() => list_.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}