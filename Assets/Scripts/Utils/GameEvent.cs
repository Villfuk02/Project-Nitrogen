using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public abstract class AbstractGameEvent<TBool, TVoid> where TBool : Delegate where TVoid : Delegate
    {
        protected readonly List<(int priority, TBool boolHandler, TVoid voidHandler)> handlerQueue = new();

        public void Register(int priority, TBool handler) => Register(priority, handler, null);
        public void Register(int priority, TVoid handler) => Register(priority, null, handler);
        void Register(int priority, TBool boolHandler, TVoid voidHandler)
        {
            for (int i = 0; i < handlerQueue.Count; i++)
            {
                if (handlerQueue[i].priority <= priority)
                    continue;

                handlerQueue.Insert(i, (priority, boolHandler, voidHandler));
                return;
            }
            handlerQueue.Add((priority, boolHandler, voidHandler));
        }

        public void Unregister(TBool handler)
        {
            for (int i = 0; i < handlerQueue.Count; i++)
            {
                if (handlerQueue[i].boolHandler != handler)
                    continue;
                handlerQueue.RemoveAt(i);
                return;
            }

            Debug.LogWarning($"unregistering a handler that wasn't registered ({handler.Method.Name})");
        }
        public void Unregister(TVoid handler)
        {
            for (int i = 0; i < handlerQueue.Count; i++)
            {
                if (handlerQueue[i].voidHandler != handler)
                    continue;
                handlerQueue.RemoveAt(i);
                return;
            }

            Debug.LogWarning($"unregistering a handler that wasn't registered ({handler.Method.Name})");
        }
    }
    public class GameEvent : AbstractGameEvent<GameEvent.Handle, GameEvent.HandleVoid>
    {
        public delegate bool Handle();
        public delegate void HandleVoid();

        public bool Invoke()
        {
            foreach ((int priority, var boolHandler, var voidHandler) in handlerQueue)
            {
                Debug.Log($"{priority} {boolHandler?.Method.Name}{voidHandler?.Method.Name}");
                if (boolHandler == null)
                    voidHandler!.Invoke();
                else if (!boolHandler.Invoke())
                    return false;
            }

            return true;
        }
    }
    public class GameEvent<T> : AbstractGameEvent<GameEvent<T>.Handle, GameEvent<T>.HandleVoid>
    {
        public delegate bool Handle(ref T data);
        public delegate void HandleVoid(ref T data);
        public bool Invoke(ref T data)
        {
            foreach ((int priority, var boolHandler, var voidHandler) in handlerQueue)
            {
                Debug.Log($"{priority} {boolHandler?.Method.Name}{voidHandler?.Method.Name} input: {data}");
                if (boolHandler == null)
                    voidHandler!.Invoke(ref data);
                else if (!boolHandler.Invoke(ref data))
                    return false;
            }

            return true;
        }
    }
}
