using System;

namespace Utils
{
    public abstract class AbstractModifiableCommand<THandler, TReaction> where THandler : Delegate where TReaction : Delegate
    {
        protected readonly OrderedList<int, THandler> modifiers = new();
        protected readonly OrderedList<int, TReaction> reactions = new();
        protected THandler handler;
        public void RegisterReaction(TReaction reaction, int priority)
        {
            if (priority <= 0) throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be positive");
            reactions.Add(priority, reaction);
        }

        public void UnregisterReaction(TReaction reaction) => reactions.Remove(reaction);

        public void RegisterModifier(THandler modifier, int priority)
        {
            if (priority >= 0) throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be negative");
            modifiers.Add(priority, modifier);
        }

        public void UnregisterModifier(THandler modifier) => modifiers.Remove(modifier);

        public void RegisterHandler(THandler handler)
        {
            if (this.handler != null)
                throw new InvalidOperationException("A handler was already registered");
            this.handler = handler;
        }

        public void UnregisterHandler(THandler handler)
        {
            if (this.handler != handler)
                throw new InvalidOperationException("This handler is not the one that was registered");
            this.handler = null;
        }
    }

    public class ModifiableCommand : AbstractModifiableCommand<ModifiableCommand.Handler, ModifiableCommand.Reaction>
    {
        public delegate bool Handler();
        public delegate void Reaction();

        public bool Invoke()
        {
            foreach (var (_, modifier) in modifiers)
                if (!modifier.Invoke())
                    return false;

            if (handler != null && !handler.Invoke())
                return false;

            foreach (var (_, reaction) in reactions)
                reaction.Invoke();
            return true;
        }
    }

    public class ModifiableCommand<T> : AbstractModifiableCommand<ModifiableCommand<T>.Handler, ModifiableCommand<T>.Reaction>
    {
        public delegate bool Handler(ref T data);
        public delegate void Reaction(T data);

        public bool Invoke(T data) => InvokeRef(ref data);

        public bool InvokeRef(ref T data)
        {
            foreach (var (_, modifier) in modifiers)
                if (!modifier.Invoke(ref data))
                    return false;

            if (handler != null && !handler.Invoke(ref data))
                return false;

            foreach (var (_, reaction) in reactions)
                reaction.Invoke(data);
            return true;
        }
    }
}
