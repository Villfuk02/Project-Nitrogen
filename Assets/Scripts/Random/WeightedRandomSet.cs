
using System;
using System.Collections.Generic;

namespace Random
{
    public class WeightedRandomSet<T>
    {
        readonly List<(T item, float weight)> list_;
        readonly Dictionary<T, int> positions_;
        readonly Random random_;
        float totalWeight_;
        public int Count { get => list_.Count; }
        public IEnumerable<(T, float)> AllEntries => list_;
        public WeightedRandomSet(ulong randomSeed)
        {
            list_ = new();
            positions_ = new();
            random_ = new(randomSeed);
            totalWeight_ = 0;
        }
        public WeightedRandomSet(WeightedRandomSet<T> original)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(original.random_.CurrentState());
            totalWeight_ = original.totalWeight_;
        }
        public WeightedRandomSet(WeightedRandomSet<T> original, ulong randomSeed)
        {
            list_ = new(original.list_);
            positions_ = new(original.positions_);
            random_ = new(randomSeed);
            totalWeight_ = original.totalWeight_;
        }
        public WeightedRandomSet(IEnumerable<(T item, float weight)> items, ulong randomSeed) : this(randomSeed)
        {
            foreach ((T item, float weight) in items)
            {
                Add(item, weight);
            }
        }

        public void Add(T item, float weight)
        {
            if (positions_.ContainsKey(item))
                throw new ArgumentException($"Set already contains item {item}.");
            if (weight < 0)
                throw new ArgumentException("Weight cannot be negative.");
            positions_.Add(item, list_.Count);
            list_.Add((item, weight));
            totalWeight_ += weight;
        }
        public void Remove(T item)
        {
            if (!positions_.ContainsKey(item))
                throw new ArgumentException($"Set does not contain item {item}.");
            int pos = positions_[item];
            totalWeight_ -= list_[pos].weight;
            positions_.Remove(item);
            if (pos != list_.Count - 1)
            {
                list_[pos] = list_[^1];
                positions_[list_[pos].item] = pos;
            }
            list_.RemoveAt(list_.Count - 1);
        }
        public T PopRandom()
        {
            if (totalWeight_ <= 0)
                throw new InvalidOperationException("Cannot pop from an empty set.");
            float r = random_.FloatExclusive(0, totalWeight_);
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
        public void UpdateWeight(T item, float newWeight)
        {
            if (!positions_.ContainsKey(item))
                return;
            int pos = positions_[item];
            totalWeight_ -= list_[pos].weight;
            totalWeight_ += newWeight;
            list_[pos] = (item, newWeight);
        }
        public bool Contains(T item)
        {
            return positions_.ContainsKey(item);
        }
        public void Clear()
        {
            list_.Clear();
            positions_.Clear();
            totalWeight_ = 0;
        }
    }
}
