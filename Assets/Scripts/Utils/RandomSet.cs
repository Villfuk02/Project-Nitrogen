using System.Collections.Generic;

namespace Assets.Scripts.Utils
{
    public class RandomSet<T>
    {
        readonly List<T> _list;
        readonly Dictionary<T, int> _positions;
        readonly ThreadSafeRandom _random;
        public int Count { get => _list.Count; }
        public IEnumerable<T> AllEntries => _list;
        public RandomSet()
        {
            _list = new();
            _positions = new();
            _random = new();
        }
        public RandomSet(RandomSet<T> original)
        {
            _list = new(original._list);
            _positions = new(original._positions);
            _random = new();
        }

        public void TryAdd(T item)
        {
            if (!_positions.ContainsKey(item))
                Add(item);
        }

        public void Add(T item)
        {
            if (_positions.ContainsKey(item))
                throw new System.Exception($"Set already contains item {item}.");
            _positions.Add(item, _list.Count);
            _list.Add(item);
        }
        public void Remove(T item)
        {
            if (!_positions.ContainsKey(item))
                throw new System.Exception($"Set does not contain item {item}.");
            int pos = _positions[item];
            _positions.Remove(item);
            if (pos != _list.Count - 1)
            {
                _list[pos] = _list[^1];
                _positions[_list[pos]] = pos;
            }
            _list.RemoveAt(_list.Count - 1);
        }
        public void TryRemove(T item)
        {
            if (_positions.ContainsKey(item))
                Remove(item);
        }
        public T PopRandom()
        {
            if (_list.Count == 0)
                throw new System.Exception("Cannot pop from an empty set.");
            int r = _random.Next(_list.Count);
            T ret = _list[r];
            Remove(ret);
            return ret;
        }
        public bool Contains(T item)
        {
            return _positions.ContainsKey(item);
        }
        public void Clear()
        {
            _list.Clear();
            _positions.Clear();
        }
    }
}