using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils.Random
{
    /// <summary>
    /// Represents an unordered set of weighted items of type T, being able to return them in a random order. Each item's chance to be returned is proportional to its weight.
    /// </summary>
    public class WeightedRandomSet<T> : IEnumerable<(T item, float weight)>
    {
        //underlying container of items
        readonly List<(T item, float weight)> list_;
        //maps items to their position in list_
        readonly Dictionary<T, int> positions_;
        readonly Random random_;
        float totalWeight_;
        public int Count { get => list_.Count; }
        public bool IsReadOnly => false;
        /// <summary>
        /// Creates a new empty instance of <see cref="WeightedRandomSet{T}"/>.
        /// </summary>
        /// <param name="randomSeed">Seed for the random number generator.</param>
        public WeightedRandomSet(ulong randomSeed)
        {
            list_ = new();
            positions_ = new();
            random_ = new(randomSeed);
            totalWeight_ = 0;
        }
        /// <summary>
        /// Creates a copy of another <see cref="WeightedRandomSet{T}"/>, copying both its contents and random number generator to provide items in the same order.
        /// </summary>
        /// <param name="original">The set to copy.</param>
        public WeightedRandomSet(WeightedRandomSet<T> original)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(original.random_.CurrentState);
            totalWeight_ = original.totalWeight_;
        }
        /// <summary>
        /// Creates a new instance of <see cref="WeightedRandomSet{T}"/>, filled with the item-weight pairs from the provided container.
        /// </summary>
        /// <param name="items">The container to take items and weights from.</param>
        /// <param name="randomSeed">Seed for the random number generator.</param>
        public WeightedRandomSet(IEnumerable<(T item, float weight)> items, ulong randomSeed) : this(randomSeed)
        {
            foreach ((T item, float weight) in items)
            {
                Add(item, weight);
            }
        }
        /// <summary>
        /// Creates a copy of another <see cref="WeightedRandomSet{T}"/>, but with a different random number generator.
        /// </summary>
        /// <param name="original">The set to copy.</param>
        /// <param name="randomSeed">Seed for the random number generator.</param>
        public WeightedRandomSet(WeightedRandomSet<T> original, ulong randomSeed)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(randomSeed);
            totalWeight_ = original.totalWeight_;
        }
        /// <summary>
        /// Adds a new item to the set, if not present.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <param name="weight">Positive weight. The greater weight, the greater chance to be selected.</param>
        public void Add(T item, float weight)
        {
            if (positions_.ContainsKey(item))
                return;
            if (weight <= 0)
                throw new ArgumentException("Weight must be positive.");
            positions_.Add(item, list_.Count);
            list_.Add((item, weight));
            totalWeight_ += weight;
        }
        /// <summary>
        /// Removes an item from the set, if present.
        /// </summary>
        /// <returns>true, if the item was present, false otherwise.</returns>
        public bool Remove(T item)
        {
            if (!positions_.ContainsKey(item))
                return false;
            int pos = positions_[item];
            totalWeight_ -= list_[pos].weight;
            positions_.Remove(item);
            if (pos != list_.Count - 1)
            {
                list_[pos] = list_[^1];
                positions_[list_[pos].item] = pos;
            }
            list_.RemoveAt(list_.Count - 1);
            return true;
        }
        /// <summary>
        /// Gets a random item from the set and removes it. Throws an exception when the set is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public T PopRandom()
        {
            if (totalWeight_ <= 0)
                throw new InvalidOperationException("Cannot pop from an empty set.");
            float r = random_.Float(0, totalWeight_);
            int pos = 0;
            for (int i = 0; i < list_.Count; i++)
            {
                r -= list_[i].weight;
                if (r >= 0)
                    continue;
                pos = i;
                break;
            }
            T ret = list_[pos].item;
            Remove(ret);
            return ret;
        }

        /// <summary>
        /// Changes the weight of an item already in the set.
        /// </summary>
        /// <param name="item">Item to update. Throws an exception when the item isn't present.</param>
        /// <param name="newWeight">Positive weight. The greater weight, the greater chance to be selected.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UpdateWeight(T item, float newWeight)
        {
            if (newWeight <= 0)
                throw new ArgumentException("Weight must be positive.");
            if (!positions_.ContainsKey(item))
                throw new InvalidOperationException($"Item {item} was not present in the set.");
            int pos = positions_[item];
            totalWeight_ -= list_[pos].weight;
            totalWeight_ += newWeight;
            list_[pos] = (item, newWeight);
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
            totalWeight_ = 0;
        }
        public IEnumerator<(T item, float weight)> GetEnumerator() => list_.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
