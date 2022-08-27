// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// MODIFIED
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
namespace System.Collections.Generic
{
    [DebuggerDisplay("Count = {Count}")]
    public class PriorityQueue<TElement, TPriority>
    {
        private (TElement Element, TPriority Priority)[] _nodes;
        private readonly IComparer<TPriority>? _comparer;
        private UnorderedItemsCollection? _unorderedItems;
        private int _size;
        private int _version;
        private const int Arity = 4;
        private const int Log2Arity = 2;
        private readonly Dictionary<TElement, int> _positions;

        public PriorityQueue()
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            _comparer = InitializeComparer(null);
            _positions = new();
        }
        public PriorityQueue(int initialCapacity) : this(initialCapacity, comparer: null) { }
        public PriorityQueue(IComparer<TPriority>? comparer)
        {
            _nodes = Array.Empty<(TElement, TPriority)>();
            _comparer = InitializeComparer(comparer);
            _positions = new();
        }
        public PriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialCapacity), initialCapacity, "");
            }
            _nodes = new (TElement, TPriority)[initialCapacity];
            _comparer = InitializeComparer(comparer);
            _positions = new(initialCapacity);
        }
        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items) : this(items, comparer: null) { }
        public PriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> items, IComparer<TPriority>? comparer)
        {
            if (items == null)
                throw new ArgumentNullException();
            _nodes = items.ToArray();
            _size = _nodes.Length;
            _comparer = InitializeComparer(comparer);
            _positions = new(items.Select((x, i) => new KeyValuePair<TElement, int>(x.Element, i)));
            if (_size > 1)
            {
                Heapify();
            }
        }
        public PriorityQueue(PriorityQueue<TElement, TPriority> original)
        {
            _nodes = ((TElement Element, TPriority Priority)[])original._nodes.Clone();
            _comparer = InitializeComparer(original._comparer);
            _size = original._size;
            _version = original._version;
            _positions = new(original._positions);
        }
        public int Count => _size;
        public IComparer<TPriority> Comparer => _comparer ?? Comparer<TPriority>.Default;
        public UnorderedItemsCollection UnorderedItems => _unorderedItems ??= new UnorderedItemsCollection(this);
        public void Enqueue(TElement element, TPriority priority)
        {
            // Virtually add the node at the end of the underlying array.
            // Note that the node being enqueued does not need to be physically placed
            // there at this point, as such an assignment would be redundant.
            int currentSize = _size++;
            _version++;
            if (_nodes.Length == currentSize)
            {
                Grow(currentSize + 1);
            }
            if (_comparer == null)
            {
                MoveUpDefaultComparer((element, priority), currentSize);
            }
            else
            {
                MoveUpCustomComparer((element, priority), currentSize);
            }
        }
        public TElement Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }
            return _nodes[0].Element;
        }
        public TElement Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }
            TElement element = _nodes[0].Element;
            RemoveRootNode(element);
            return element;
        }
        public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (_size != 0)
            {
                (element, priority) = _nodes[0];
                RemoveRootNode(element);
                return true;
            }
            element = default;
            priority = default;
            return false;
        }
        public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
        {
            if (_size != 0)
            {
                (element, priority) = _nodes[0];
                return true;
            }
            element = default;
            priority = default;
            return false;
        }
        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            if (_size != 0)
            {
                (TElement Element, TPriority Priority) root = _nodes[0];
                if (_comparer == null)
                {
                    if (Comparer<TPriority>.Default.Compare(priority, root.Priority) > 0)
                    {
                        MoveDownDefaultComparer((element, priority), 0);
                        _version++;
                        return root.Element;
                    }
                }
                else
                {
                    if (_comparer.Compare(priority, root.Priority) > 0)
                    {
                        MoveDownCustomComparer((element, priority), 0);
                        _version++;
                        return root.Element;
                    }
                }
            }
            return element;
        }
        public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> items)
        {
            if (items == null)
                throw new ArgumentNullException();
            int count = 0;
            var collection = items as ICollection<(TElement Element, TPriority Priority)>;
            if (collection is not null && (count = collection.Count) > _nodes.Length - _size)
            {
                Grow(_size + count);
            }
            if (_size == 0)
            {
                // build using Heapify() if the queue is empty.
                if (collection is not null)
                {
                    collection.CopyTo(_nodes, 0);
                    _size = count;
                }
                else
                {
                    int i = 0;
                    (TElement, TPriority)[] nodes = _nodes;
                    foreach ((TElement element, TPriority priority) in items)
                    {
                        if (nodes.Length == i)
                        {
                            Grow(i + 1);
                            nodes = _nodes;
                        }
                        nodes[i++] = (element, priority);
                    }
                    _size = i;
                }
                _version++;
                if (_size > 1)
                {
                    Heapify();
                }
            }
            else
            {
                foreach ((TElement element, TPriority priority) in items)
                {
                    Enqueue(element, priority);
                }
            }
        }
        public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
        {
            if (elements == null)
                throw new ArgumentNullException();
            int count;
            if (elements is ICollection<(TElement Element, TPriority Priority)> collection &&
                (count = collection.Count) > _nodes.Length - _size)
            {
                Grow(_size + count);
            }
            if (_size == 0)
            {
                // build using Heapify() if the queue is empty.
                int i = 0;
                (TElement, TPriority)[] nodes = _nodes;
                foreach (TElement element in elements)
                {
                    if (nodes.Length == i)
                    {
                        Grow(i + 1);
                        nodes = _nodes;
                    }
                    nodes[i++] = (element, priority);
                }
                _size = i;
                _version++;
                if (i > 1)
                {
                    Heapify();
                }
            }
            else
            {
                foreach (TElement element in elements)
                {
                    Enqueue(element, priority);
                }
            }
        }
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
            {
                // Clear the elements so that the gc can reclaim the references
                Array.Clear(_nodes, 0, _size);
            }
            _positions.Clear();
            _size = 0;
            _version++;
        }
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "");
            }
            if (_nodes.Length < capacity)
            {
                Grow(capacity);
                _version++;
            }
            return _nodes.Length;
        }
        public void RemoveElement(TElement element)
        {
            int elementIndex = _positions[element];
            _positions.Remove(element);
            int lastNodeIndex = --_size;
            _version++;
            if (lastNodeIndex > 0)
            {
                (TElement Element, TPriority Priority) lastNode = _nodes[lastNodeIndex];
                if (_comparer == null)
                {
                    MoveDownDefaultComparer(lastNode, elementIndex);
                }
                else
                {
                    MoveDownCustomComparer(lastNode, elementIndex);
                }
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
            {
                _nodes[lastNodeIndex] = default;
            }
        }
        public void ChangePriority(TElement element, TPriority priority)
        {
            if (!_positions.ContainsKey(element))
                throw new Exception("Cannot change priority of element not contained in the queue.");
            int elementIndex = _positions[element];
            if (_comparer == null)
            {
                if (Comparer<TPriority>.Default.Compare(priority, _nodes[elementIndex].Priority) > 0)
                {
                    MoveDownDefaultComparer((element, priority), elementIndex);
                }
                else
                {
                    MoveUpDefaultComparer((element, priority), elementIndex);
                }
            }
            else
            {
                if (_comparer.Compare(priority, _nodes[elementIndex].Priority) > 0)
                {
                    MoveDownCustomComparer((element, priority), elementIndex);
                }
                else
                {
                    MoveUpCustomComparer((element, priority), elementIndex);
                }
            }
        }
        public bool Contains(TElement item)
        {
            return _positions.ContainsKey(item);
        }
        public TPriority PeekPriority(TElement item)
        {
            return _nodes[_positions[item]].Priority;
        }
        public void TrimExcess()
        {
            int threshold = (int)(_nodes.Length * 0.9);
            if (_size < threshold)
            {
                Array.Resize(ref _nodes, _size);
                _version++;
            }
        }
        private void Grow(int minCapacity)
        {
            Debug.Assert(_nodes.Length < minCapacity);
            const int GrowFactor = 2;
            const int MinimumGrow = 4;
            int newcapacity = GrowFactor * _nodes.Length;
            // Allow the queue to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _nodes.Length overflowed thanks to the (uint) cast
            if ((uint)newcapacity > 2_000_000_000) newcapacity = 2_000_000_000;
            // Ensure minimum growth is respected.
            newcapacity = Math.Max(newcapacity, _nodes.Length + MinimumGrow);
            // If the computed capacity is still less than specified, set to the original argument.
            // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
            if (newcapacity < minCapacity) newcapacity = minCapacity;
            Array.Resize(ref _nodes, newcapacity);
        }
        private void RemoveRootNode(TElement removedElement)
        {
            _positions.Remove(removedElement);
            int lastNodeIndex = --_size;
            _version++;
            if (lastNodeIndex > 0)
            {
                (TElement Element, TPriority Priority) lastNode = _nodes[lastNodeIndex];
                if (_comparer == null)
                {
                    MoveDownDefaultComparer(lastNode, 0);
                }
                else
                {
                    MoveDownCustomComparer(lastNode, 0);
                }
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>())
            {
                _nodes[lastNodeIndex] = default;
            }
        }
        private static int GetParentIndex(int index) => (index - 1) >> Log2Arity;
        private static int GetFirstChildIndex(int index) => (index << Log2Arity) + 1;
        private void Heapify()
        {
            // Leaves of the tree are in fact 1-element heaps, for which there
            // is no need to correct them. The heap property needs to be restored
            // only for higher nodes, starting from the first node that has children.
            // It is the parent of the very last element in the array.
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int lastParentWithChildren = GetParentIndex(_size - 1);
            if (_comparer == null)
            {
                for (int index = lastParentWithChildren; index >= 0; --index)
                {
                    MoveDownDefaultComparer(nodes[index], index);
                }
            }
            else
            {
                for (int index = lastParentWithChildren; index >= 0; --index)
                {
                    MoveDownCustomComparer(nodes[index], index);
                }
            }
        }
        private void MoveUpDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // Instead of swapping items all the way to the root, we will perform
            // a similar optimization as in the insertion sort.
            Debug.Assert(_comparer is null);
            Debug.Assert(0 <= nodeIndex && nodeIndex < _size);
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            while (nodeIndex > 0)
            {
                int parentIndex = GetParentIndex(nodeIndex);
                (TElement Element, TPriority Priority) parent = nodes[parentIndex];
                if (Comparer<TPriority>.Default.Compare(node.Priority, parent.Priority) < 0)
                {
                    nodes[nodeIndex] = parent;
                    _positions[parent.Element] = nodeIndex;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }
            nodes[nodeIndex] = node;
            _positions[node.Element] = nodeIndex;
        }
        private void MoveUpCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // Instead of swapping items all the way to the root, we will perform
            // a similar optimization as in the insertion sort.
            Debug.Assert(_comparer is not null);
            Debug.Assert(0 <= nodeIndex && nodeIndex < _size);
            IComparer<TPriority> comparer = _comparer;
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            while (nodeIndex > 0)
            {
                int parentIndex = GetParentIndex(nodeIndex);
                (TElement Element, TPriority Priority) parent = nodes[parentIndex];
                if (comparer.Compare(node.Priority, parent.Priority) < 0)
                {
                    nodes[nodeIndex] = parent;
                    _positions[parent.Element] = nodeIndex;
                    nodeIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }
            nodes[nodeIndex] = node;
            _positions[node.Element] = nodeIndex;
        }
        private void MoveDownDefaultComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // The node to move down will not actually be swapped every time.
            // Rather, values on the affected path will be moved up, thus leaving a free spot
            // for this value to drop in. Similar optimization as in the insertion sort.
            Debug.Assert(_comparer is null);
            Debug.Assert(0 <= nodeIndex && nodeIndex < _size);
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int size = _size;
            int i;
            while ((i = GetFirstChildIndex(nodeIndex)) < size)
            {
                // Find the child node with the minimal priority
                (TElement Element, TPriority Priority) minChild = nodes[i];
                int minChildIndex = i;
                int childIndexUpperBound = Math.Min(i + Arity, size);
                while (++i < childIndexUpperBound)
                {
                    (TElement Element, TPriority Priority) nextChild = nodes[i];
                    if (Comparer<TPriority>.Default.Compare(nextChild.Priority, minChild.Priority) < 0)
                    {
                        minChild = nextChild;
                        minChildIndex = i;
                    }
                }
                // Heap property is satisfied; insert node in this location.
                if (Comparer<TPriority>.Default.Compare(node.Priority, minChild.Priority) <= 0)
                {
                    break;
                }
                // Move the minimal child up by one node and
                // continue recursively from its location.
                nodes[nodeIndex] = minChild;
                _positions[minChild.Element] = nodeIndex;
                nodeIndex = minChildIndex;
            }
            nodes[nodeIndex] = node;
            _positions[node.Element] = nodeIndex;
        }
        private void MoveDownCustomComparer((TElement Element, TPriority Priority) node, int nodeIndex)
        {
            // The node to move down will not actually be swapped every time.
            // Rather, values on the affected path will be moved up, thus leaving a free spot
            // for this value to drop in. Similar optimization as in the insertion sort.
            Debug.Assert(_comparer is not null);
            Debug.Assert(0 <= nodeIndex && nodeIndex < _size);
            IComparer<TPriority> comparer = _comparer;
            (TElement Element, TPriority Priority)[] nodes = _nodes;
            int size = _size;
            int i;
            while ((i = GetFirstChildIndex(nodeIndex)) < size)
            {
                // Find the child node with the minimal priority
                (TElement Element, TPriority Priority) minChild = nodes[i];
                int minChildIndex = i;
                int childIndexUpperBound = Math.Min(i + Arity, size);
                while (++i < childIndexUpperBound)
                {
                    (TElement Element, TPriority Priority) nextChild = nodes[i];
                    if (comparer.Compare(nextChild.Priority, minChild.Priority) < 0)
                    {
                        minChild = nextChild;
                        minChildIndex = i;
                    }
                }
                // Heap property is satisfied; insert node in this location.
                if (comparer.Compare(node.Priority, minChild.Priority) <= 0)
                {
                    break;
                }
                // Move the minimal child up by one node and continue recursively from its location.
                nodes[nodeIndex] = minChild;
                _positions[minChild.Element] = nodeIndex;
                nodeIndex = minChildIndex;
            }
            nodes[nodeIndex] = node;
            _positions[node.Element] = nodeIndex;
        }
        private static IComparer<TPriority>? InitializeComparer(IComparer<TPriority>? comparer)
        {
            if (typeof(TPriority).IsValueType)
            {
                if (comparer == Comparer<TPriority>.Default)
                {
                    // if the user manually specifies the default comparer,
                    // revert to using the optimized path.
                    return null;
                }
                return comparer;
            }
            else
            {
                // Currently the JIT doesn't optimize direct Comparer<T>.Default.Compare
                // calls for reference types, so we want to cache the comparer instance instead.
                // TODO https://github.com/dotnet/runtime/issues/10050: Update if this changes in the future.
                return comparer ?? Comparer<TPriority>.Default;
            }
        }
        [DebuggerDisplay("Count = {Count}")]
        public sealed class UnorderedItemsCollection : IReadOnlyCollection<(TElement Element, TPriority Priority)>, ICollection
        {
            internal readonly PriorityQueue<TElement, TPriority> _queue;
            internal UnorderedItemsCollection(PriorityQueue<TElement, TPriority> queue) => _queue = queue;
            public int Count => _queue._size;
            object ICollection.SyncRoot => this;
            bool ICollection.IsSynchronized => false;
            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException();
                if (array.Rank != 1)
                {
                    throw new ArgumentException();
                }
                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentException();
                }
                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "");
                }
                if (array.Length - index < _queue._size)
                {
                    throw new ArgumentException();
                }
                try
                {
                    Array.Copy(_queue._nodes, 0, array, index, _queue._size);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }
            public struct Enumerator : IEnumerator<(TElement Element, TPriority Priority)>
            {
                private readonly PriorityQueue<TElement, TPriority> _queue;
                private readonly int _version;
                private int _index;
                private (TElement, TPriority) _current;
                internal Enumerator(PriorityQueue<TElement, TPriority> queue)
                {
                    _queue = queue;
                    _index = 0;
                    _version = queue._version;
                    _current = default;
                }
                public void Dispose() { }
                public bool MoveNext()
                {
                    PriorityQueue<TElement, TPriority> localQueue = _queue;
                    if (_version == localQueue._version && ((uint)_index < (uint)localQueue._size))
                    {
                        _current = localQueue._nodes[_index];
                        _index++;
                        return true;
                    }
                    return MoveNextRare();
                }
                private bool MoveNextRare()
                {
                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException();
                    }
                    _index = _queue._size + 1;
                    _current = default;
                    return false;
                }
                public (TElement Element, TPriority Priority) Current => _current;
                object IEnumerator.Current => _current;
                void IEnumerator.Reset()
                {
                    if (_version != _queue._version)
                    {
                        throw new InvalidOperationException();
                    }
                    _index = 0;
                    _current = default;
                }
            }
            public Enumerator GetEnumerator() => new Enumerator(_queue);
            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}