
namespace Utils
{
    public class FixedCapacityStack<T>
    {
        readonly T[] array_;
        readonly int capacity_;
        int index_;

        public int Count { get; private set; }

        public FixedCapacityStack(int capacity)
        {
            array_ = new T[capacity];
            capacity_ = capacity;
            Count = 0;
            index_ = -1;
        }

        public void Push(T item)
        {
            index_ = (index_ + 1) % capacity_;
            array_[index_] = item;
            if (Count < capacity_) Count++;
        }
        public T Pop()
        {
            if (Count == 0)
                throw new System.InvalidOperationException("Cannot pop from empty stack.");
            T r = array_[index_];
            index_ = (index_ + capacity_ - 1) % capacity_;
            Count--;
            return r;
        }
    }
}
