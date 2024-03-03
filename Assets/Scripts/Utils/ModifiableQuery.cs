using System;

namespace Utils
{
    public class ModifiableQuery<TData, TResult>
    {
        public delegate void Modifier(ref TData data);
        public delegate TResult Acceptor(TData data);
        readonly OrderedList<int, Modifier> modifiers_ = new();
        Acceptor acceptor_;

        public void RegisterModifier(Modifier modifier, int priority)
        {
            if (priority >= 0) throw new ArgumentException("priority must be negative");
            modifiers_.Add(priority, modifier);
        }

        public void UnregisterModifier(Modifier modifier) => modifiers_.Remove(modifier);
        public void RegisterAcceptor(Acceptor acceptor)
        {
            if (acceptor_ != null)
                throw new InvalidOperationException("An acceptor was already registered");
            acceptor_ = acceptor;
        }

        public void UnregisterAcceptor(Acceptor acceptor)
        {
            if (acceptor_ != acceptor)
                throw new InvalidOperationException("This acceptor is not the one that was registered");
            acceptor_ = null;
        }

        public TResult Query(TData data)
        {
            if (acceptor_ == null)
                throw new InvalidOperationException("No acceptor was registered");

            foreach (var (_, modifier) in modifiers_)
                modifier.Invoke(ref data);

            return acceptor_.Invoke(data);
        }
    }
}
