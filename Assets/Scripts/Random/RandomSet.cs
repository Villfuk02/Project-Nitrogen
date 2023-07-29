
using System;
using System.Collections.Generic;

namespace Random
{
    public class RandomSet<T>
    {
        readonly List<T> list_;
        readonly Dictionary<T, int> positions_;
        readonly Random random_;
        public int Count { get => list_.Count; }
        public IEnumerable<T> AllEntries => list_;
        public RandomSet(ulong randomSeed)
        {
            list_ = new();
            positions_ = new();
            random_ = new(randomSeed);
        }
        public RandomSet(RandomSet<T> original)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(original.random_.CurrentState());
        }
        public RandomSet(IEnumerable<T> items, ulong randomSeed) : this(randomSeed)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void TryAdd(T item)
        {
            if (!positions_.ContainsKey(item))
                Add(item);
        }

        public void Add(T item)
        {
            if (positions_.ContainsKey(item))
                throw new ArgumentException($"Set already contains item {item}.");
            positions_.Add(item, list_.Count);
            list_.Add(item);
        }
        public void Remove(T item)
        {
            if (!positions_.ContainsKey(item))
                throw new ArgumentException($"Set does not contain item {item}.");
            int pos = positions_[item];
            positions_.Remove(item);
            if (pos != list_.Count - 1)
            {
                list_[pos] = list_[^1];
                positions_[list_[pos]] = pos;
            }
            list_.RemoveAt(list_.Count - 1);
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
        public void TryRemove(T item)
        {
            if (positions_.ContainsKey(item))
                Remove(item);
        }
        public T PopRandom()
        {
            if (list_.Count == 0)
                throw new InvalidOperationException("Cannot pop from an empty set.");
            int r = random_.Int(list_.Count);
            T ret = list_[r];
            Remove(ret);
            return ret;
        }
        public bool Contains(T item)
        {
            return positions_.ContainsKey(item);
        }
        public void Clear()
        {
            list_.Clear();
            positions_.Clear();
        }
    }
}