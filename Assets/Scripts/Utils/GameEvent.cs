using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public abstract class AbstractGameEvent<T> where T : Delegate
    {
        protected readonly List<(int priority, T handler)> handlerQueue = new();

        public void Register(T handler, int priority)
        {
            for (int i = 0; i < handlerQueue.Count; i++)
            {
                if (handlerQueue[i].priority <= priority)
                    continue;

                handlerQueue.Insert(i, (priority, handler));
                return;
            }
            handlerQueue.Add((priority, handler));
        }

        public void Unregister(T handler)
        {
            for (int i = 0; i < handlerQueue.Count; i++)
            {
                if (handlerQueue[i].handler != handler)
                    continue;
                handlerQueue.RemoveAt(i);
                return;
            }

            Debug.LogWarning($"Unregistering a handler that wasn't registered ({(handler.Method.Name)})");
        }

        public void Clear()
        {
            handlerQueue.Clear();
        }
    }
    public class GameEvent : AbstractGameEvent<GameEvent.Handle>
    {
        public delegate void Handle();

        public void Broadcast()
        {
            foreach ((int priority, var handler) in handlerQueue)
            {
                Debug.Log($"{priority} {handler.Method.Name}");
                handler.Invoke();
            }
        }
    }
    public class GameEvent<T> : AbstractGameEvent<GameEvent<T>.Handle>
    {
        public delegate void Handle(T data);
        public void Broadcast(T data)
        {
            foreach ((int priority, var handler) in handlerQueue)
            {
                Debug.Log($"{priority} {handler.Method.Name}");
                handler.Invoke(data);
            }
        }
    }
    public class GameCommand : AbstractGameEvent<GameCommand.Handle>
    {
        public delegate bool Handle();

        public bool Invoke()
        {
            foreach ((int priority, var handler) in handlerQueue)
            {
                Debug.Log($"{priority} {handler.Method.Name}");
                if (!handler.Invoke())
                {
                    Debug.Log("Command blocked");
                    return false;
                }
            }
            return true;
        }
    }
    public class GameCommand<T> : AbstractGameEvent<GameCommand<T>.Handle>
    {
        public delegate bool Handle(ref T data);

        public bool Invoke(T data) => InvokeRef(ref data);
        public bool InvokeRef(ref T data)
        {
            foreach ((int priority, var handler) in handlerQueue)
            {
                Debug.Log($"{priority} {handler.Method.Name}");
                if (!handler.Invoke(ref data))
                {
                    Debug.Log("Command blocked");
                    return false;
                }
            }
            return true;
        }
    }
    public class GameQuery<T> : AbstractGameEvent<GameQuery<T>.Handle>
    {
        public delegate void Handle(ref T data);
        public delegate bool Accept(ref T data);
        Accept accept_;

        public void RegisterAcceptor(Accept accept)
        {
            if (accept_ is not null)
                throw new InvalidOperationException($"Acceptor already registered ({accept_.Method.Name}).");
            accept_ = accept;
        }

        public void UnregisterAcceptor()
        {
            if (accept_ is null)
                throw new InvalidOperationException("No acceptor registered.");
            accept_ = null;
        }
        public bool Query(ref T data)
        {
            foreach ((int priority, var handler) in handlerQueue)
            {
                Debug.Log($"{priority} {handler.Method.Name}");
                handler.Invoke(ref data);
            }

            return accept_ is null || accept_(ref data);
        }
    }
}
