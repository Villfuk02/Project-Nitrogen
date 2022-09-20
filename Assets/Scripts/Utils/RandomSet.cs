using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.Utils
{
    public class RandomSet<T>
    {
        readonly List<T> _list;
        readonly Dictionary<T, int> _positions;
        public int Count { get => _list.Count; }
        public IEnumerable<T> AllEntries => _list;
        public RandomSet()
        {
            _list = new();
            _positions = new();
        }
        public RandomSet(RandomSet<T> original)
        {
            _list = new(original._list);
            _positions = new(original._positions);
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
            int r = Random.Range(0, _list.Count);
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