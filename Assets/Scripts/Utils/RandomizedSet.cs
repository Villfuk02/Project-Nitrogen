using System.Collections.Generic;
using UnityEngine;

public class RandomizedSet<T>
{
    readonly List<T> _list;
    readonly HashSet<T> _set;
    public int Count { get => _list.Count; }

    public RandomizedSet()
    {
        _list = new();
        _set = new();
    }
    public RandomizedSet(RandomizedSet<T> original)
    {
        _list = new(original._list);
        _set = new(original._set);
    }

    public void Add(T item)
    {
        if (_set.Contains(item))
            return;
        _set.Add(item);
        _list.Add(item);
    }
    public T PopRandom()
    {
        int r = Random.Range(0, _list.Count);
        T ret = _list[r];
        _set.Remove(ret);
        _list[r] = _list[Count - 1];
        _list.RemoveAt(Count - 1);
        return ret;
    }
    public bool Contains(T item)
    {
        return _set.Contains(item);
    }
    public void Clear()
    {
        _list.Clear();
        _set.Clear();
    }
}
