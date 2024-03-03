using System;
using System.Collections;
using System.Collections.Generic;

namespace Utils
{
    public class OrderedList<TKey, TElement> : IEnumerable<OrderedList<TKey, TElement>.Entry>
    {
        public readonly struct Entry
        {
            public readonly TKey key;
            public readonly TElement element;

            public Entry(TKey key, TElement element)
            {
                this.key = key;
                this.element = element;
            }

            public void Deconstruct(out TKey key, out TElement element)
            {
                key = this.key;
                element = this.element;
            }
        }
        class Section
        {
            public LinkedListNode<Entry> start;
            public LinkedListNode<Entry> end;

            public Section(LinkedListNode<Entry> start, LinkedListNode<Entry> end)
            {
                this.start = start;
                this.end = end;
            }
        }

        public int Count => elementNodes_.Count;

        readonly LinkedList<Entry> list_ = new();
        readonly SortedList<TKey, Section> sections_ = new();
        readonly Dictionary<TElement, LinkedListNode<Entry>> elementNodes_ = new();

        public void Add(TKey key, TElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (elementNodes_.ContainsKey(element))
                throw new ArgumentException("This list already contains the specified element");

            Entry e = new(key, element);

            LinkedListNode<Entry> node;
            if (!sections_.ContainsKey(key))
            {
                Section s = new(null, null);
                sections_.Add(key, s);
                int sectionIndex = sections_.IndexOfKey(key);
                node = sectionIndex == 0 ? list_.AddFirst(e) : list_.AddAfter(sections_.Values[sectionIndex - 1].end, e);
                s.start = node;
                s.end = node;
            }
            else
            {
                Section s = sections_[key];
                node = list_.AddAfter(s.end, e);
                s.end = node;
            }
            elementNodes_.Add(element, node);
        }

        public void Remove(TElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (!elementNodes_.TryGetValue(element, out var node))
                throw new ArgumentException("This list does not contain the specified element");

            elementNodes_.Remove(element);
            var key = node.Value.key;
            var s = sections_[key];
            if (s.start == node && s.end == node)
                sections_.Remove(key);
            else if (s.start == node)
                s.start = node.Next;
            else if (s.end == node)
                s.end = node.Previous;

            list_.Remove(node);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<Entry> GetEnumerator() => list_.GetEnumerator();
    }
}
