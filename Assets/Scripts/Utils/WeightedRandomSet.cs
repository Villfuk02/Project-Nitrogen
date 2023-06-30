
using System.Collections.Generic;

namespace Utils
{
    public class WeightedRandomSet<T>
    {
        readonly List<(T item, float weight)> _list;
        readonly Dictionary<T, int> _positions;
        readonly ThreadSafeRandom _random;
        float totalWeight;
        public int Count { get => _list.Count; }
        public IEnumerable<(T, float)> AllEntries => _list;
        public WeightedRandomSet()
        {
            _list = new();
            _positions = new();
            _random = new();
            totalWeight = 0;
        }
        public WeightedRandomSet(WeightedRandomSet<T> original)
        {
            _list = new(original._list);
            _positions = new(original._positions);
            _random = new();
            totalWeight = original.totalWeight;
        }

        public void Add(T item, float weight)
        {
            if (_positions.ContainsKey(item))
                throw new System.Exception($"Set already contains item {item}.");
            _positions.Add(item, _list.Count);
            _list.Add((item, weight));
            totalWeight += weight;
        }
        public void Remove(T item)
        {
            if (!_positions.ContainsKey(item))
                throw new System.Exception($"Set does not contain item {item}.");
            int pos = _positions[item];
            totalWeight -= _list[pos].weight;
            _positions.Remove(item);
            if (pos != _list.Count - 1)
            {
                _list[pos] = _list[^1];
                _positions[_list[pos].item] = pos;
            }
            _list.RemoveAt(_list.Count - 1);
        }
        public T PopRandom()
        {
            float r = _random.NextFloat(0, totalWeight);
            int pos = 0;
            for (int i = 0; i < _list.Count; i++)
            {
                r -= _list[i].weight;
                if (r < 0)
                {
                    pos = i;
                    break;
                }
            }
            T ret = _list[pos].item;
            Remove(ret);
            return ret;
        }
        public void UpdateWeight(T item, float newWeight)
        {
            if (!_positions.ContainsKey(item))
                return;
            int pos = _positions[item];
            totalWeight -= _list[pos].weight;
            totalWeight += newWeight;
            _list[pos] = (item, newWeight);
        }
        public bool Contains(T item)
        {
            return _positions.ContainsKey(item);
        }
        public void Clear()
        {
            _list.Clear();
            _positions.Clear();
            totalWeight = 0;
        }
    }
}
