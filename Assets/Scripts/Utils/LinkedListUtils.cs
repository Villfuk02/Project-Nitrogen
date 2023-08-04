using System;
using System.Collections.Generic;

namespace Utils
{
    public static class LinkedListUtils
    {
        /// <summary>
        /// Reverse the order of nodes in a linked list.
        /// </summary>
        public static void Reverse<T>(this LinkedList<T> list)
        {
            if (list.Count <= 1)
                return;
            Reverse(list, list.First, list.Last);
        }
        /// <summary>
        /// Reverses the order the nodes in between 'from' and 'to', including them. 'from' and 'to' must be within the linked list in this order.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public static void Reverse<T>(this LinkedList<T> list, LinkedListNode<T> from, LinkedListNode<T> to)
        {
            if (list.Count == 0)
                throw new ArgumentException("List is empty");
            if (from == to)
                return;

            //find start of region to reverse
            // > > > [from] > > > to > > >
            var current = list.First!;
            while (current != from)
                current = current.Next ?? throw new ArgumentException("From was not found in List");

            //insert reversed region before 'from'
            // > > > to < < < [from] > > > to > > >
            var currentReversed = to;
            while (currentReversed != from)
            {
                list.AddBefore(current, currentReversed.Value);
                currentReversed = currentReversed.Previous ?? throw new ArgumentException("From was not before To");
            }

            //skip 'from'
            // > > > to < < < from [>] > > to > > >
            current = current.Next!;

            //remove unreversed region
            // > > > to < < < from [to] > > >
            while (current != to)
            {
                var next = current.Next ?? throw new ArgumentException("To was not found in List");
                list.Remove(current);
                current = next;
            }

            //remove 'to'
            // > > > to < < < from > > >
            list.Remove(current!);
        }
    }
}
