using System;

namespace Utils
{
    public class AbstractEventReactionChain<TReaction> where TReaction : Delegate
    {
        protected readonly OrderedList<int, TReaction> reactions = new();

        public void RegisterReaction(TReaction reaction, int priority)
        {
            if (priority <= 0)
                throw new ArgumentException("Priority must be positive");
            reactions.Add(priority, reaction);
        }

        public void UnregisterReaction(TReaction reaction) => reactions.Remove(reaction);
    }
    public class EventReactionChain : AbstractEventReactionChain<EventReactionChain.Reaction>
    {
        public delegate void Reaction();

        public void Broadcast()
        {
            foreach (var (_, reaction) in reactions)
                reaction.Invoke();
        }
    }

    public class EventReactionChain<T> : AbstractEventReactionChain<EventReactionChain<T>.Reaction>
    {
        public delegate void Reaction(T data);

        public void Broadcast(T data)
        {
            foreach (var (_, reaction) in reactions)
                reaction.Invoke(data);
        }
    }
}
