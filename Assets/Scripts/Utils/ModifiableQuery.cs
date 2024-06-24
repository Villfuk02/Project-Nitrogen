using System;

namespace Utils
{
    public class ModifiableQuery<TInput, TData, TResult>
    {
        public delegate void Modifier(TInput input, ref TData data);

        public delegate TData Provider(TInput input);

        public delegate TResult Acceptor(TData data);

        readonly OrderedList<int, Modifier> modifiers_ = new();
        protected Acceptor acceptor;
        readonly Provider provider_;

        public ModifiableQuery(Provider provider)
        {
            provider_ = provider;
        }

        public ModifiableQuery(Provider provider, Acceptor acceptor)
        {
            provider_ = provider;
            this.acceptor = acceptor;
        }

        public void RegisterModifier(Modifier modifier, int priority)
        {
            if (priority >= 0) throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be negative");
            modifiers_.Add(priority, modifier);
        }

        public void UnregisterModifier(Modifier modifier)
        {
            modifiers_.Remove(modifier);
        }

        public void RegisterAcceptor(Acceptor acceptor)
        {
            if (this.acceptor != null)
                throw new InvalidOperationException("An acceptor was already registered");
            this.acceptor = acceptor;
        }

        public void UnregisterAcceptor(Acceptor acceptor)
        {
            if (this.acceptor != acceptor)
                throw new InvalidOperationException("This acceptor is not the one that was registered");
            this.acceptor = null;
        }

        public TResult Query(TInput input) => Query(input, provider_(input));

        public TResult Query(TInput input, TData customData)
        {
            if (acceptor == null)
                throw new InvalidOperationException("No acceptor was registered");

            TData data = customData;

            foreach (var (_, modifier) in modifiers_)
                modifier.Invoke(input, ref data);

            var result = acceptor.Invoke(data);
            return result;
        }
    }

    public class ModifiableQuery<TInput, TData> : ModifiableQuery<TInput, TData, TData>
    {
        public ModifiableQuery(Provider provider) : base(provider)
        {
            acceptor = data => data;
        }
    }
}