namespace Assets.Scripts.Utils
{
    public class FixedStack<T>
    {
        readonly T[] _array;
        readonly int _size;
        int _count;
        int _index;

        public int Count { get => _count; }

        public FixedStack(int depth)
        {
            _array = new T[depth];
            _size = depth;
            _count = 0;
            _index = -1;
        }

        public void Push(T item)
        {
            _index = (_index + 1) % _size;
            _array[_index] = item;
            if (_count < _size) _count++;
        }
        public T Pop()
        {
            if (_count == 0)
                throw new System.Exception("Cannot pop from empty stack");
            T r = _array[_index];
            _index = (_index + _size - 1) % _size;
            _count--;
            return r;
        }
    }
}
