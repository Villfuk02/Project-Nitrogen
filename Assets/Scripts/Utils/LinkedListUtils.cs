using System;
using System.Collections.Generic;

namespace Utils
{
    public static class LinkedListUtils
    {
        public static void Reverse<T>(this LinkedList<T> list)
        {
            if (list.Count <= 1)
                return;
            Reverse(list, list.First, list.Last);
        }
        public static void Reverse<T>(this LinkedList<T> list, LinkedListNode<T> from, LinkedListNode<T> to)
        {
            if (list.Count == 0)
                throw new ArgumentException("List is empty");
            if (from == to)
                return;

            //find start of region to reverse
            var current = list.First!;
            while (current != from)
                current = current.Next ?? throw new ArgumentException("From was not found in List");

            //insert reversed region
            var currentReversed = to;
            while (currentReversed != from)
            {
                list.AddBefore(current, currentReversed.Value);
                currentReversed = currentReversed.Previous ?? throw new ArgumentException("From was not before To");
            }

            //skip From
            current = current.Next ?? throw new ArgumentException("To was no found in List");

            //remove unreversed region
            while (current != to)
            {
                var next = current.Next ?? throw new ArgumentException("To was no found in List");
                list.Remove(current);
                current = next;
            }

            //remove To
            list.Remove(current);
        }
    }
}
